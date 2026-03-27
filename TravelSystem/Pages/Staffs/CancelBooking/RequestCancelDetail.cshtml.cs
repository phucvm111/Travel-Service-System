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
                .Include(r => r.Book.User) // Đảm bảo lấy được thông tin khách
                .FirstOrDefaultAsync(r => r.RequestCancelId == requestId);

            if (request == null) return NotFound();

            decimal refundAmount = CalculateRefundAmount(request);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy ví hệ thống (Admin ID: 1) và ví khách
                var systemWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == 1);
                var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.Book.UserId);

                if (systemWallet == null || systemWallet.Balance < refundAmount)
                {
                    TempData["Error"] = "Ví hệ thống không đủ số dư để hoàn tiền!";
                    return RedirectToPage(new { id = requestId });
                }

                // 2. Cập nhật số dư 2 ví
                systemWallet.Balance -= refundAmount;
                userWallet.Balance += refundAmount;

                // 3. Ghi lịch sử giao dịch cho HỆ THỐNG (BỔ SUNG TẠI ĐÂY)
                _context.TransactionHistories.Add(new TransactionHistory
                {
                    UserId = 1, // ID của hệ thống
                    Amount = refundAmount,
                    TransactionType = "REFUND",
                    Description = $"Hoàn tiền đơn #{request.Book.BookCode} cho khách {request.Book.Gmail} | [Ví tổng: {systemWallet.Balance:N0}đ]",
                    TransactionDate = DateTime.Now
                });

                // 4. Ghi lịch sử giao dịch cho KHÁCH HÀNG
                _context.TransactionHistories.Add(new TransactionHistory
                {
                    UserId = request.Book.UserId,
                    Amount = refundAmount,
                    TransactionType = "RECEIVE", // Khách nhận tiền hoàn
                    Description = $"Nhận tiền hoàn tour {request.Book.TourDeparture.Tour.TourName} | [Số dư: {userWallet.Balance:N0}đ]",
                    TransactionDate = DateTime.Now
                });

                // 5. Cập nhật trạng thái yêu cầu và đơn hàng
                request.Status = "FINISHED";
                request.Book.Status = 6; // Đã hoàn tiền

                // 6. Trả lại chỗ trống cho Tour
                int seats = (request.Book.NumberAdult ?? 0) + (request.Book.NumberChildren ?? 0);
                request.Book.TourDeparture.AvailableSeat += seats;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Gửi Email thông báo
                string content = $"<h3>Chào {request.Book.FirstName},</h3><p>Yêu cầu hủy tour <b>{request.Book.TourDeparture.Tour.TourName}</b> đã được duyệt.</p><p>Số tiền <b>{refundAmount:N0} VNĐ</b> đã được hoàn trả vào ví của bạn.</p>";
                await _emailService.SendAsync(request.Book.Gmail, "Xác nhận huỷ chuyến đi thành công", content);

                return RedirectToPage(new { id = requestId, statusAction = "approved" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToPage(new { id = requestId });
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