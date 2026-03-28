using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs
{
    public class MainDashboardModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public MainDashboardModel(FinalPrnContext context) => _context = context;

        public User SystemAccount { get; set; }
        public int NumberOfFinishedTourToPay { get; set; }
        public decimal TotalCommission { get; set; }
        public List<Booking> FinishedBookings { get; set; }

        [BindProperty(SupportsGet = true)] public DateTime FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime ToDate { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string DateError { get; set; }

        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            // Kiểm tra quyền Staff
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 4) return RedirectToPage("/Auths/Login");

            CurrentPage = p;
            int pageSize = 10;

            // 1. Lấy ví hệ thống (Sửa ID 1 thành 6 nếu DB của bạn ví hệ thống là 6)
            SystemAccount = await _context.Users.FirstOrDefaultAsync(w => w.UserId == 1);

            // 2. Thiết lập ngày mặc định
            if (FromDate == DateTime.MinValue || ToDate == DateTime.MinValue)
            {
                ToDate = DateTime.Now;
                FromDate = DateTime.Now.AddDays(-30);
            }

            if (ToDate < FromDate)
            {
                DateError = "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";
                return Page();
            }

            // CHỖ QUAN TRỌNG: Chuyển DateTime sang DateOnly để SQL hiểu được
            DateOnly start = DateOnly.FromDateTime(FromDate);
            DateOnly end = DateOnly.FromDateTime(ToDate);

            // 3. Truy vấn Booking với logic so sánh DateOnly
            var query = _context.Bookings
                    .Include(b => b.TourDeparture)
                        .ThenInclude(td => td.Tour)
                        .ThenInclude(t => t.TravelAgent)
                    .Where(b => (b.Status == 4 || b.Status == 5) && // Thêm ngoặc ở đây
                                b.BookDate != null &&
                                b.BookDate >= start &&
                                b.BookDate <= end);

            // 4. Số chuyến đi hoàn thành tổng quát
            NumberOfFinishedTourToPay = await _context.Bookings.CountAsync(b => b.Status == 4);

            // 5. Tính toán hoa hồng (Lưu ý: TotalPrice trong DB của bạn là float nên cần ép sang decimal)
            var allFinished = await query.ToListAsync();
            TotalCommission = allFinished.Sum(b => (decimal)(b.TotalPrice ?? 0) * 0.2m);

            // 6. Phân trang
            int totalItems = allFinished.Count;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            FinishedBookings = allFinished
                .OrderByDescending(b => b.BookDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostPayAllAsync()
        {
            // 1. Lấy danh sách booking hoàn thành (status = 4)
            var pendingBookings = await _context.Bookings
                .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.Tour)
                .Where(b => b.Status == 4)
                .ToListAsync();

            if (!pendingBookings.Any())
            {
                TempData["SuccessMessage"] = "Không có chuyến đi nào cần quyết toán.";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy ví hệ thống (ID 1)
                var systemWallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == 1);
                if (systemWallet == null) throw new Exception("Không tìm thấy ví hệ thống.");

                foreach (var booking in pendingBookings)
                {
                    // Lấy đại lý chủ tour
                    int agentId = booking.TourDeparture.Tour.TravelAgentId;
                    var agentWallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == agentId);
                    if (agentWallet == null) continue;

                    decimal total = (decimal)(booking.TotalPrice ?? 0);
                    decimal payToAgent = total * 0.8m; // Trả 80% cho Agent

                    // Cập nhật số dư
                    systemWallet.Balance -= payToAgent;
                    agentWallet.Balance += payToAgent;

                    // Ghi Log hệ thống (Chi trả)
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = 1,
                        Amount = payToAgent,
                        TransactionType = "PAYMENT",
                        Description = $"Thanh toán Tour #{booking.BookCode} cho Agent | [Ví tổng: {systemWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    // Ghi Log Đại lý (Thu nhập)
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = agentId,
                        Amount = payToAgent,
                        TransactionType = "INCOME",
                        Description = $"Nhận quyết toán Tour #{booking.BookCode} | [Số dư: {agentWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    // CHỐT: Đánh dấu đơn đã thanh toán xong cho đại lý
                    booking.Status = 5;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Đã quyết toán thành công {pendingBookings.Count} đơn cho các đại lý!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi quyết toán: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}