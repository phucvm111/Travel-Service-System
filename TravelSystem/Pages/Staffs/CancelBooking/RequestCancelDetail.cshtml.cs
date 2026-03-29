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
                .Include(r => r.Staff).ThenInclude(s => s.StaffNavigation) // Lấy thông tin Staff và User xử lý
                .FirstOrDefaultAsync(m => m.RequestCancelId == id);

            if (RequestDetail == null) return NotFound();

            CalculatedRefund = CalculateRefundAmount(RequestDetail);
            return Page();
        }

        // --- XỬ LÝ DUYỆT & HOÀN TIỀN ---
        public async Task<IActionResult> OnPostApproveAsync(int requestId)
        {
            int? currentStaffId = HttpContext.Session.GetInt32("UserID"); // Lấy ID người đang đăng nhập
            if (currentStaffId == null) return RedirectToPage("/Auths/Login");

            var request = await _context.RequestCancels
                .Include(r => r.Book).ThenInclude(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .Include(r => r.Book.User)
                .FirstOrDefaultAsync(r => r.RequestCancelId == requestId);

            if (request == null) return NotFound();

            decimal refundAmount = CalculateRefundAmount(request);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var systemWallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == 1);
                var userWallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == request.Book.UserId);

                if (systemWallet == null || systemWallet.Balance < refundAmount)
                {
                    TempData["Error"] = "Ví hệ thống không đủ số dư để hoàn tiền!";
                    return RedirectToPage(new { id = requestId });
                }

                // 1. Cập nhật số dư
                systemWallet.Balance -= refundAmount;
                userWallet.Balance += refundAmount;

                // 2. Ghi lịch sử giao dịch
                _context.TransactionHistories.Add(new TransactionHistory
                {
                    UserId = 1,
                    Amount = refundAmount,
                    TransactionType = "REFUND",
                    Description = $"Hoàn tiền đơn #{request.Book.BookCode} cho khách {request.Book.Gmail}",
                    TransactionDate = DateTime.Now
                });

                _context.TransactionHistories.Add(new TransactionHistory
                {
                    UserId = request.Book.UserId,
                    Amount = refundAmount,
                    TransactionType = "RECEIVE",
                    Description = $"Nhận tiền hoàn tour {request.Book.TourDeparture.Tour.TourName}",
                    TransactionDate = DateTime.Now
                });

                // 3. Cập nhật trạng thái và GÁN STAFF ID XỬ LÝ
                request.Status = "FINISHED";
                request.StaffId = currentStaffId; // Lưu người đã bấm duyệt

                request.Book.Status = 6;

                int seats = (request.Book.NumberAdult ?? 0) + (request.Book.NumberChildren ?? 0);
                request.Book.TourDeparture.AvailableSeat += seats;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Gửi Email
                string content = $"<h3>Chào {request.Book.FirstName},</h3><p>Yêu cầu hủy tour <b>{request.Book.TourDeparture.Tour.TourName}</b> đã được duyệt bởi nhân viên.</p><p>Số tiền <b>{refundAmount:N0} VNĐ</b> đã được hoàn trả.</p>";
                await _emailService.SendAsync(request.Book.Gmail, "Xác nhận huỷ chuyến đi thành công", content);

                return RedirectToPage(new { id = requestId });
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
            int? currentStaffId = HttpContext.Session.GetInt32("UserID");
            if (currentStaffId == null) return RedirectToPage("/Auths/Login");

            var request = await _context.RequestCancels
                .Include(r => r.Book).ThenInclude(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .FirstOrDefaultAsync(r => r.RequestCancelId == requestId);

            if (request == null) return NotFound();

            // Cập nhật trạng thái và GÁN STAFF ID XỬ LÝ
            request.Status = "REJECTED";
            request.StaffId = currentStaffId; // Lưu người đã bấm từ chối

            request.Book.Status = 1;
            request.Book.Note = $"[Staff từ chối hủy]: {rejectReason} | " + request.Book.Note;

            await _context.SaveChangesAsync();

            // Gửi Email
            string content = $"<h3>Chào {request.Book.FirstName},</h3><p>Yêu cầu hủy tour <b>{request.Book.TourDeparture.Tour.TourName}</b> bị từ chối.</p><p>Lý do: {rejectReason}</p>";
            await _emailService.SendAsync(request.Book.Gmail, "Yêu cầu huỷ chuyến đi bị từ chối", content);

            return RedirectToPage(new { id = requestId });
        }

        private decimal CalculateRefundAmount(RequestCancel r)
        {
            if (r.Book == null || r.Book.TourDeparture == null) return 0;
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