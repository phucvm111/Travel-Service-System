using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Admin.CancelBookingTourist
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
        public double RefundAmount { get; set; }
        [TempData] public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            RequestDetail = await _context.RequestCancels
                .Include(r => r.Book)
                    .ThenInclude(b => b.Tour)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.RequestCancelId == id);

            if (RequestDetail == null) return NotFound();

            RefundAmount = CalculateRefund(RequestDetail);
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestCancelID)
        {
            var request = await _context.RequestCancels
                .Include(r => r.Book).ThenInclude(b => b.Tour)
                .FirstOrDefaultAsync(r => r.RequestCancelId == requestCancelID);

            if (request == null) return NotFound();

            double refund = CalculateRefund(request);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cập nhật trạng thái yêu cầu hủy
                request.Status = "FINISHED";

                // 2. Cộng tiền vào ví người dùng
                var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
                if (userWallet == null) throw new Exception("Không tìm thấy ví người dùng");

                userWallet.Balance += (decimal)refund;

                // 3. Lưu lịch sử giao dịch
                var history = new TransactionHistory
                {
                    UserId = request.UserId,
                    Amount = (decimal)refund,
                    TransactionType = "REFUND",
                    Description = $"Hoàn tiền hủy tour: {request.Book?.Tour?.TourName}",
                    TransactionDate = DateTime.Now,
                    AmountAfterTransaction = userWallet.Balance
                };
                _context.TransactionHistories.Add(history);

                // 4. Cập nhật trạng thái đơn hàng (Status 6: Đã hoàn tiền)
                if (request.Book != null)
                {
                    request.Book.Status = 6;
                    // Trả lại số lượng chỗ cho Tour
                    if (request.Book.Tour != null)
                    {
                        request.Book.Tour.Quantity += (request.Book.NumberAdult + request.Book.NumberChildren);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Gửi Email
                string emailBody = $"Chào {request.Book?.FirstName}, yêu cầu hủy tour {request.Book?.Tour?.TourName} đã được duyệt. Số tiền hoàn: {refund:N0} VNĐ đã được cộng vào ví.";
                await _emailService.SendAsync(request.Book?.Gmail, "Thông báo hoàn tiền hủy chuyến", emailBody, true);

                return RedirectToPage(new { id = requestCancelID, approve = "true" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ErrorMessage = "Lỗi xử lý: " + ex.Message;
                return RedirectToPage(new { id = requestCancelID, approve = "false" });
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestCancelID, string rejectReason)
        {
            var request = await _context.RequestCancels.Include(r => r.Book).FirstOrDefaultAsync(r => r.RequestCancelId == requestCancelID);
            if (request == null) return NotFound();

            request.Status = "REJECTED";
            await _context.SaveChangesAsync();

            string emailBody = $"Yêu cầu hủy tour của bạn đã bị từ chối. Lý do: {rejectReason}";
            await _emailService.SendAsync(request.Book?.Gmail, "Yêu cầu hủy chuyến bị từ chối", emailBody, true);

            return RedirectToPage(new { id = requestCancelID, reject = "true" });
        }

        private double CalculateRefund(RequestCancel r)
        {
            if (r.Book == null || r.Book.Tour == null) return 0;
            var requestDate = r.RequestDate ?? DateOnly.FromDateTime(DateTime.Now);
            var bookingDate = r.Book.BookDate ?? requestDate;
            var startDate = r.Book.Tour.StartDay ?? requestDate;
            double totalPrice = r.Book.TotalPrice ?? 0;

            int daysToStart = startDate.DayNumber - requestDate.DayNumber;

            // Logic hoàn tiền (24h đầu 100%, >7 ngày 80%, >3 ngày 50%)
            if (requestDate == bookingDate) return totalPrice;
            if (daysToStart >= 7) return totalPrice * 0.8;
            if (daysToStart >= 3) return totalPrice * 0.5;
            return 0;
        }
    }
}