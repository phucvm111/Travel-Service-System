using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Agents.TourBooked
{
    public class ManageTourBookedModel : PageModel
    {
        private readonly FinalPrnContext _context;

        private readonly IEmailService _emailService;
        public ManageTourBookedModel(FinalPrnContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
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
        public async Task<IActionResult> OnPostCancelAsync(int bookId, string reason, string? voucherCode)
        {
            // 1. Load dữ liệu kèm thông tin Đại lý sở hữu Tour
            var booking = await _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(td => td.Tour).ThenInclude(t => t.TravelAgent)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (booking == null) return NotFound();

            // 2. Validate trạng thái đơn hàng
            if (booking.Status != 1 && booking.Status != 7)
            {
                TempData["Error"] = "Đơn hàng không ở trạng thái hợp lệ để hủy.";
                return RedirectToPage();
            }

            // 3. --- VALIDATE VOUCHER CỦA ĐẠI LÝ ---
            string voucherHtml = "";
            if (!string.IsNullOrEmpty(voucherCode))
            {
                // Kiểm tra voucher: Phải đúng mã, Status = 2 (Agent), và thuộc về Agent sở hữu tour này
                var v = await _context.Vouchers.FirstOrDefaultAsync(x =>
                    x.VoucherCode == voucherCode &&
                    x.Status == 2 &&
                    x.UserId == booking.TourDeparture.Tour.TravelAgent.UserId);

                if (v == null)
                {
                    // Chúng ta không dùng TempData nữa mà truyền trực tiếp vào tham số Redirect
                    string errorMsg = $"Mã voucher '{voucherCode}' không tồn tại hoặc không thuộc quyền sở hữu của bạn.";
                    return RedirectToPage(new { cancel = "voucherfalse", msg = errorMsg });
                }

                // Nếu có voucher hợp lệ, chuẩn bị nội dung tặng quà trong Email
                voucherHtml = $@"
            <div style='padding:15px; border:2px dashed #28a745; background:#f9fff9; margin-top:15px; border-radius:8px;'>
                <p style='color:#28a745; font-weight:bold; margin:0; font-size:16px;'>🎁 Quà tặng xin lỗi từ Đại lý:</p>
                <p style='font-size:22px; margin:10px 0; color:#1a1a1a;'>Mã giảm giá: <b style='color:#d9534f;'>{v.VoucherCode}</b></p>
                <p style='font-size:14px; color:#555;'>Ưu đãi: <b>Giảm {v.PercentDiscount}%</b> cho lần đặt tour tiếp theo của bạn tại đại lý <b>{booking.TourDeparture.Tour.TravelAgent.TravelAgentName}</b>.</p>
            </div>";
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 4. Xử lý ví tiền và Log (Nếu đơn đã thanh toán)
                if (booking.Status == 1)
                {
                    decimal refundAmount = (decimal)(booking.TotalPrice ?? 0);
                    var systemWallet = await _context.Users.FirstOrDefaultAsync(u => u.UserId == 1);
                    var userWallet = await _context.Users.FirstOrDefaultAsync(u => u.UserId == booking.UserId);

                    if (systemWallet == null || systemWallet.Balance < refundAmount)
                        throw new Exception("Ví hệ thống không đủ số dư để hoàn tiền.");

                    systemWallet.Balance -= refundAmount;
                    userWallet.Balance += refundAmount;

                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = 1,
                        Amount = refundAmount,
                        TransactionType = "REFUND",
                        Description = $"Hoàn tiền đơn #{booking.BookCode} (Agent hủy) | [Ví tổng: {systemWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = booking.UserId,
                        Amount = refundAmount,
                        TransactionType = "RECEIVE",
                        Description = $"Nhận tiền hoàn 100% từ Agent (Đơn #{booking.BookCode}) | [Số dư: {userWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    _context.RequestCancels.Add(new RequestCancel
                    {
                        BookId = booking.BookId,
                        RequestDate = DateOnly.FromDateTime(DateTime.Now),
                        Reason = $"[Đại lý hủy]: {reason}" + (string.IsNullOrEmpty(voucherCode) ? "" : $" (Kèm Voucher: {voucherCode})"),
                        Status = "FINISHED"
                    });
                }

                // 5. Cập nhật trạng thái Booking và trả chỗ
                booking.Status = 3;
                booking.Note = $"[Agent hủy]: {reason} " + (string.IsNullOrEmpty(voucherCode) ? "" : $"| Voucher tặng: {voucherCode} ") + $"| {booking.Note}";

                int totalPeople = (booking.NumberAdult ?? 0) + (booking.NumberChildren ?? 0);
                booking.TourDeparture.AvailableSeat += totalPeople;

                await _context.SaveChangesAsync();

                // 6. Gửi Email thông báo (ĐẶT TRONG TRANSACTION)
                string emailBody = $@"
            <div style='font-family:Arial,sans-serif; color:#333; line-height:1.6; max-width:600px; margin:auto; border:1px solid #eee; padding:20px;'>
                <h2 style='color:#d9534f; border-bottom:2px solid #d9534f; padding-bottom:10px;'>Thông báo hủy chuyến đi</h2>
                <p>Chào <b>{booking.FirstName} {booking.LastName}</b>,</p>
                <p>Đại lý <b>{booking.TourDeparture.Tour.TravelAgent.TravelAgentName}</b> rất tiếc phải thông báo rằng tour <b>{booking.TourDeparture.Tour.TourName}</b> của bạn đã bị hủy.</p>
                <p><b>Lý do hủy:</b> <i>{reason}</i></p>
                <p style='background:#f8f9fa; padding:10px;'>Hệ thống đã hoàn trả <b>100% số tiền ({booking.TotalPrice:N0} VNĐ)</b> vào ví cá nhân của bạn.</p>
                {voucherHtml}
                <p style='margin-top:25px; border-top:1px solid #eee; padding-top:15px; font-size:13px;'>
                    Mọi thắc mắc vui lòng liên hệ Hotline: <b>{booking.TourDeparture.Tour.TravelAgent.HotLine}</b><br/>
                    Trân trọng,<br/><b>Đội ngũ GoViet & {booking.TourDeparture.Tour.TravelAgent.TravelAgentName}</b>
                </p>
            </div>";

                await _emailService.SendAsync(booking.Gmail, $"[GoViet] Xác nhận hủy chuyến đi #{booking.BookCode}", emailBody, true);

                // Commit mọi thứ
                await transaction.CommitAsync();

                TempData["Success"] = "Đã hủy đơn, hoàn tiền và tặng voucher thành công.";
                return RedirectToPage(new { cancel = "true" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToPage();
            }
        }
    }
}