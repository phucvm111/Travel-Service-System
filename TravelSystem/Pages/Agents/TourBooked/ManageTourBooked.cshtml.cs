using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.TourBooked
{
    public class ManageTourBookedModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public ManageTourBookedModel(FinalPrnContext context) => _context = context;

        public List<Booking> BookingList { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int StartIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? StatusType { get; set; }

        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            // 1. Kiểm tra quyền Agent (RoleId = 3)
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            int? agentUserId = HttpContext.Session.GetInt32("UserID");
            if (roleId != 3 || agentUserId == null) return RedirectToPage("/Auths/Login");

            // 2. Lấy TravelAgentID của User hiện tại
            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == agentUserId);
            if (agent == null) return NotFound("Thông tin đại lý không tồn tại.");

            CurrentPage = p;
            int pageSize = 5;

            // 3. Xây dựng truy vấn
            var query = _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .Include(b => b.Voucher)
                .Where(b => b.TourDeparture.Tour.TravelAgentId == agent.TravelAgentId)
                .AsQueryable();

            // Lọc theo tên khách hàng
            if (!string.IsNullOrEmpty(SearchName))
            {
                string lowerSearch = SearchName.ToLower();
                query = query.Where(b => (b.FirstName + " " + b.LastName).ToLower().Contains(lowerSearch));
            }

            // Lọc theo trạng thái
            if (StatusType.HasValue)
            {
                query = query.Where(b => b.Status == StatusType.Value);
            }

            // 4. Phân trang
            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            StartIndex = (CurrentPage - 1) * pageSize + 1;

            BookingList = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        // Action xử lý Đại lý chủ động hủy tour
        public async Task<IActionResult> OnPostCancelAsync(int bookId, string reason)
        {
            // 1. Lấy thông tin đơn hàng kèm thông tin Tour và User
            var booking = await _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(td => td.Tour)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (booking == null) return NotFound();

            // 2. Chỉ cho phép hủy nếu đơn đang ở trạng thái 1 (Đã thanh toán) hoặc 7 (Chờ thanh toán)
            if (booking.Status != 1 && booking.Status != 7)
            {
                TempData["Error"] = "Đơn hàng không ở trạng thái hợp lệ để hủy.";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Nếu khách đã thanh toán (Status 1) thì mới thực hiện hoàn tiền
                if (booking.Status == 1)
                {
                    decimal refundAmount = (decimal)(booking.TotalPrice ?? 0);

                    // Lấy ví hệ thống (Admin ID: 1) và ví khách
                    var systemWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == 1);
                    var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == booking.UserId);

                    if (systemWallet == null || systemWallet.Balance < refundAmount)
                    {
                        TempData["Error"] = "Ví hệ thống không đủ số dư để hoàn tiền cho khách!";
                        return RedirectToPage();
                    }

                    // A. Cập nhật số dư ví
                    systemWallet.Balance -= refundAmount;
                    userWallet.Balance += refundAmount;

                    // B. Ghi lịch sử giao dịch cho HỆ THỐNG (Admin)
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = 1,
                        Amount = refundAmount,
                        TransactionType = "REFUND",
                        Description = $"Hoàn tiền đơn #{booking.BookCode} do Agent hủy | [Ví tổng: {systemWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    // C. Ghi lịch sử giao dịch cho KHÁCH HÀNG
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = booking.UserId,
                        Amount = refundAmount,
                        TransactionType = "RECEIVE",
                        Description = $"Nhận tiền hoàn 100% từ Agent (Đơn #{booking.BookCode}) | [Số dư: {userWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    // D. Tạo yêu cầu hủy trong bảng RequestCancel để Staff tiện theo dõi (Status FINISHED)
                    _context.RequestCancels.Add(new RequestCancel
                    {
                        BookId = booking.BookId,
                        RequestDate = DateOnly.FromDateTime(DateTime.Now),
                        Reason = "[Agent hủy]: " + reason,
                        Status = "FINISHED"
                    });
                }

                // 3. Cập nhật trạng thái Booking và trả lại chỗ trống cho Tour
                booking.Status = 3; // Bị hủy bởi đại lý
                booking.Note = $"[Agent hủy]: {reason} | " + booking.Note;

                int seats = (booking.NumberAdult ?? 0) + (booking.NumberChildren ?? 0);
                booking.TourDeparture.AvailableSeat += seats;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Đã hủy đơn hàng và hoàn tiền thành công cho khách hàng.";
                return RedirectToPage(new { cancel = "true" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi xử lý: " + ex.Message;
                return RedirectToPage();
            }
        }
    }
}