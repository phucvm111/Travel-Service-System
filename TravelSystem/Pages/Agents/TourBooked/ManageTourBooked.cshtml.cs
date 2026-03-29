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
        public List<Voucher> AgentVouchers { get; set; }
        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            // 1. Kiểm tra quyền Agent (RoleId = 3)
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            int? agentUserId = HttpContext.Session.GetInt32("UserID");
            if (roleId != 3 || agentUserId == null) return RedirectToPage("/Auths/Login");

            // 2. Lấy TravelAgentID của User hiện tại
            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == agentUserId);
            if (agent == null) return NotFound("Thông tin đại lý không tồn tại.");

            AgentVouchers = await _context.Vouchers
                .Where(v => v.UserId == agentUserId && v.Status == 2 && v.Quantity > 0 && v.EndDate >= DateOnly.FromDateTime(DateTime.Now))
                .ToListAsync();

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
        // Action xử lý Đại lý chủ động hủy tour - Nhận mảng string[] selectedVouchers từ checkbox
        public async Task<IActionResult> OnPostCancelAsync(int bookId, string reason, string[]? selectedVouchers)
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

            // 3. --- XỬ LÝ DANH SÁCH VOUCHER ĐÃ CHỌN ---
            string vouchersHtml = ""; // Dùng để nhúng vào Email
            string voucherCodesString = ""; // Dùng để lưu vào Note của Booking

            if (selectedVouchers != null && selectedVouchers.Length > 0)
            {
                vouchersHtml = @"<div style='margin-top:20px; padding:15px; border:2px dashed #28a745; background-color:#f9fff9; border-radius:10px;'>
                            <p style='color:#28a745; font-weight:bold; margin:0; font-size:16px;'>🎁 Quà tặng tri ân đặc biệt từ Đại lý:</p>";

                foreach (var code in selectedVouchers)
                {
                    // Kiểm tra từng mã trong mảng chọn xem có hợp lệ với Agent này không
                    var v = await _context.Vouchers.FirstOrDefaultAsync(x =>
                        x.VoucherCode == code &&
                        x.Status == 2 &&
                        x.UserId == booking.TourDeparture.Tour.TravelAgent.UserId);

                    if (v != null)
                    {
                        vouchersHtml += $@"
                    <div style='margin-top:10px; border-bottom:1px solid #eee; padding-bottom:5px;'>
                        <span style='font-size:18px; color:#d9534f;'>Mã: <b>{v.VoucherCode}</b></span><br/>
                        <small style='color:#555;'>Ưu đãi: Giảm {v.PercentDiscount}% cho chuyến đi tiếp theo.</small>
                    </div>";

                        voucherCodesString += v.VoucherCode + ", ";
                    }
                }
                vouchersHtml += "</div>";
                voucherCodesString = voucherCodesString.TrimEnd(' ', ',');
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 4. Xử lý hoàn tiền (Nếu đã thanh toán)
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
                        Reason = $"[Đại lý hủy]: {reason}" + (string.IsNullOrEmpty(voucherCodesString) ? "" : $" (Kèm Voucher: {voucherCodesString})"),
                        Status = "FINISHED"
                    });
                }

                // 5. Cập nhật trạng thái Booking
                booking.Status = 3;
                booking.Note = $"[Agent hủy]: {reason} " + (string.IsNullOrEmpty(voucherCodesString) ? "" : $"| Voucher tặng: {voucherCodesString} ") + $"| {booking.Note}";

                int totalPeople = (booking.NumberAdult ?? 0) + (booking.NumberChildren ?? 0);
                booking.TourDeparture.AvailableSeat += totalPeople;

                await _context.SaveChangesAsync();

                // 6. Gửi Email thông báo
                string emailBody = $@"
            <div style='font-family:Arial,sans-serif; color:#333; line-height:1.6; max-width:600px; margin:auto; border:1px solid #eee; padding:20px;'>
                <h2 style='color:#d9534f; border-bottom:2px solid #d9534f; padding-bottom:10px;'>Thông báo hủy chuyến đi</h2>
                <p>Chào <b>{booking.FirstName} {booking.LastName}</b>,</p>
                <p>Đại lý <b>{booking.TourDeparture.Tour.TravelAgent.TravelAgentName}</b> rất tiếc phải thông báo tour <b>{booking.TourDeparture.Tour.TourName}</b> của bạn đã bị hủy.</p>
                <p><b>Lý do hủy:</b> <i>{reason}</i></p>
                <p style='background:#f8f9fa; padding:10px;'>Hệ thống đã hoàn trả <b>100% số tiền ({booking.TotalPrice:N0} VNĐ)</b> vào ví của bạn.</p>
                
                {vouchersHtml}

                <p style='margin-top:25px; border-top:1px solid #eee; padding-top:15px; font-size:13px;'>
                    Hotline hỗ trợ: <b>{booking.TourDeparture.Tour.TravelAgent.HotLine}</b><br/>
                    Trân trọng,<br/><b>Đội ngũ GoViet & {booking.TourDeparture.Tour.TravelAgent.TravelAgentName}</b>
                </p>
            </div>";

                await _emailService.SendAsync(booking.Gmail, $"[GoViet] Xác nhận hủy chuyến đi #{booking.BookCode}", emailBody, true);

                await transaction.CommitAsync();

                TempData["Success"] = "Đã hủy đơn, hoàn tiền và gửi Email kèm quà tặng thành công!";
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