using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Staffs.CancelBooking
{
    public class RequestCancelDetailModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IEmailService _emailService;

        public RequestCancelDetailModel(FinalPrnContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public RequestCancel RequestDetail { get; set; }
        public decimal CalculatedRefund { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 4 && roleId != 1) return RedirectToPage("/Auths/Login");

            RequestDetail = await _context.RequestCancels
                .Include(r => r.Book).ThenInclude(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .FirstOrDefaultAsync(m => m.RequestCancelId == id);

            if (RequestDetail == null) return NotFound();

            CalculatedRefund = CalculateRefundAmount(RequestDetail);
            return Page();
        }

        // --- XỬ LÝ DUYỆT & HOÀN TIỀN ---
        public async Task<IActionResult> OnPostApproveAsync(int requestId)
        {
            var request = await _context.RequestCancels
                .Include(r => r.Book).ThenInclude(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .FirstOrDefaultAsync(r => r.RequestCancelId == requestId);

            if (request == null) return NotFound();

            decimal refundAmount = CalculateRefundAmount(request);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra số dư ví hệ thống (Admin ID: 1)
                var systemWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == 1);
                if (systemWallet == null || systemWallet.Balance < refundAmount)
                {
                    TempData["Error"] = "Ví hệ thống không đủ số dư để hoàn tiền!";
                    return RedirectToPage(new { id = requestId });
                }

                // 2. Cập nhật trạng thái
                request.Status = "FINISHED";
                request.Book.Status = 6; // Đã hoàn tiền

                // 3. Thực hiện chuyển tiền vào ví khách
                var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.Book.UserId);
                if (userWallet != null)
                {
                    systemWallet.Balance -= refundAmount;
                    userWallet.Balance += refundAmount;

                    // 4. Ghi lịch sử giao dịch REFUND cho khách
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = request.Book.UserId,
                        Amount = refundAmount,
                        TransactionType = "REFUND",
                        Description = $"Hoàn tiền tour: {request.Book.TourDeparture.Tour.TourName}",
                        TransactionDate = DateTime.Now
                    });
                }

                // 5. Trả lại chỗ trống cho Tour
                int seats = (request.Book.NumberAdult ?? 0) + (request.Book.NumberChildren ?? 0);
                request.Book.TourDeparture.AvailableSeat += seats;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 6. Gửi Email thông báo thành công
                string content = $"<h3>Chào {request.Book.FirstName},</h3><p>Yêu cầu hủy tour <b>{request.Book.TourDeparture.Tour.TourName}</b> đã được duyệt.</p><p>Số tiền <b>{refundAmount:N0} VNĐ</b> đã được hoàn trả vào ví của bạn.</p>";
                await _emailService.SendAsync(request.Book.Gmail, "Xác nhận huỷ chuyến đi thành công", content);

                return RedirectToPage(new { id = requestId, statusAction = "approved" });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return RedirectToPage(new { id = requestId, statusAction = "failed" });
            }
        }

        // --- XỬ LÝ TỪ CHỐI ---
        public async Task<IActionResult> OnPostRejectAsync(int requestId, string rejectReason)
        {
            var request = await _context.RequestCancels
                .Include(r => r.Book).ThenInclude(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .FirstOrDefaultAsync(r => r.RequestCancelId == requestId);

            if (request == null) return NotFound();

            request.Status = "REJECTED";
            request.Book.Status = 1; // Trả về trạng thái "Đã thanh toán" bình thường
            request.Book.Note = $"[Staff từ chối hủy]: {rejectReason} | " + request.Book.Note;

            await _context.SaveChangesAsync();

            // Gửi Email thông báo từ chối
            string content = $"<h3>Chào {request.Book.FirstName},</h3><p>Yêu cầu hủy tour <b>{request.Book.TourDeparture.Tour.TourName}</b> của bạn không được chấp nhận.</p><p>Lý do: {rejectReason}</p>";
            await _emailService.SendAsync(request.Book.Gmail, "Yêu cầu huỷ chuyến đi bị từ chối", content);

            return RedirectToPage(new { id = requestId, statusAction = "rejected" });
        }

        private decimal CalculateRefundAmount(RequestCancel r)
        {
            var reqDate = r.RequestDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            var bookDate = r.Book.BookDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            var startDate = r.Book.TourDeparture.StartDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            decimal total = (decimal)(r.Book.TotalPrice ?? 0);

            if ((reqDate - bookDate).TotalHours <= 24) return total;
            if ((startDate - reqDate).TotalDays >= 7) return total * 0.8m;
            if ((startDate - reqDate).TotalDays >= 3) return total * 0.5m;
            return 0;
        }
    }
}