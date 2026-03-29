using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class MyBookingsModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _environment;

        public MyBookingsModel(FinalPrnContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IList<Booking> BookingList { get; set; } = new List<Booking>();

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; } = "current";

        [BindProperty(SupportsGet = true, Name = "p")]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 5;

        public async Task<IActionResult> OnGetAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var query = _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(td => td.Tour)
                .Include(b => b.Feedbacks)
                .Where(b => b.UserId == userId)
                .AsQueryable();

            if (Filter == "finished")
            {
                // Hiển thị tour đã xong (4) hoặc đã hoàn tiền xong (6)
                query = query.Where(b => b.Status == 4 || b.Status == 6 || b.Status == 2 || b.Status == 3);
            }
            else
            {
                // HIỂN THỊ TRONG CHUYẾN ĐI:
                // Status 1: Đã thanh toán
                // Status 5: Đã yêu cầu hoàn tiền (đang chờ duyệt)
                // Status 7: Chờ thanh toán (để khách biết còn đơn chưa trả tiền)
                query = query.Where(b => b.Status == 1 || b.Status == 5);
            }

            // ... (Phần phân trang giữ nguyên)
            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            BookingList = await query.OrderByDescending(b => b.BookDate)
                                     .Skip((CurrentPage - 1) * PageSize)
                                     .Take(PageSize).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostFeedbackAsync(int bookId, int rate, string content, IFormFile? image)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            var booking = await _context.Bookings
                .Include(b => b.Feedbacks)
                .FirstOrDefaultAsync(b => b.BookId == bookId && b.UserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToPage(new { filter = "finished" });
            }

            if (booking.Status != 4)
            {
                TempData["Error"] = "Chỉ có thể đánh giá chuyến đi đã hoàn thành.";
                return RedirectToPage(new { filter = "finished" });
            }

            if (booking.Feedbacks.Any())
            {
                TempData["Error"] = "Bạn đã đánh giá chuyến đi này rồi.";
                return RedirectToPage(new { filter = "finished" });
            }

            string? imagePath = null;

            if (image != null && image.Length > 0)
            {
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Ảnh chỉ chấp nhận jpg, jpeg, png hoặc webp.";
                    return RedirectToPage(new { filter = "finished" });
                }

                string uploadFolder = Path.Combine(_environment.WebRootPath, "images", "feedback");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string fileName = $"feedback_{bookId}_{Guid.NewGuid():N}{extension}";
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imagePath = $"/images/feedback/{fileName}";
            }

            var feedback = new Feedback
            {
                BookId = bookId,
                Rate = rate,
                Content = content?.Trim(),
                Image = imagePath,
                CreateDate = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gửi đánh giá thành công.";
            return RedirectToPage(new { filter = "finished" });
        }

        public async Task<IActionResult> OnPostCancelAsync(int bookId, string? reason)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var booking = await _context.Bookings
                .Include(b => b.TourDeparture)
                .FirstOrDefaultAsync(b => b.BookId == bookId && b.UserId == userId);

            if (booking == null) return RedirectToPage(new { filter = Filter });

            // 1. Nếu đơn hàng chưa thanh toán (Status 7), cho hủy thẳng
            if (booking.Status == 7)
            {
                booking.Status = 2; // Bạn đã hủy
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn đặt thành công.";
                return RedirectToPage(new { filter = Filter });
            }

            // 2. Kiểm tra điều kiện hủy cho đơn đã thanh toán (Status 1)
            if (booking.Status == 1)
            {
                DateTime now = DateTime.Now;
                // Chuyển đổi DateOnly sang DateTime để tính toán
                DateTime startDate = booking.TourDeparture.StartDate?.ToDateTime(TimeOnly.MinValue) ?? now;
                DateTime bookDateTime = booking.BookDate?.ToDateTime(TimeOnly.MinValue) ?? now;

                TimeSpan diffToStart = startDate - now;
                double daysToStart = diffToStart.TotalDays;

                int refundPercentage = 0;
                string policyNote = "";

                // CHÍNH SÁCH HỦY:
                // - Hoàn 100%: Trong 24h kể từ khi đặt VÀ cách khởi hành >= 7 ngày
                if ((now - bookDateTime).TotalHours <= 24 && daysToStart >= 7)
                {
                    refundPercentage = 100;
                    policyNote = "Hoàn 100% (Hủy trong 24h đầu & trước khởi hành 7 ngày)";
                }
                // - Hoàn 80%: Trước ngày khởi hành >= 7 ngày
                else if (daysToStart >= 7)
                {
                    refundPercentage = 80;
                    policyNote = "Hoàn 80% (Hủy trước khởi hành 7 ngày trở lên)";
                }
                // - Hoàn 50%: Trước ngày khởi hành từ 3 đến 6 ngày
                else if (daysToStart >= 3)
                {
                    refundPercentage = 50;
                    policyNote = "Hoàn 50% (Hủy trước khởi hành 3-6 ngày)";
                }
                // - Không được hủy: Dưới 3 ngày
                else
                {
                    TempData["Error"] = "Chuyến đi khởi hành trong vòng chưa đầy 3 ngày tới, bạn không thể hủy đơn theo chính sách của chúng tôi.";
                    return RedirectToPage(new { filter = Filter });
                }

                // Tạo yêu cầu hủy
                booking.Status = 5; // Chờ hoàn tiền
                var requestCancel = new RequestCancel
                {
                    BookId = bookId,
                    // Lưu lại tỷ lệ hoàn tiền vào Reason để Staff biết đường duyệt
                    Reason = $"[{policyNote}] Lý do khách: {reason?.Trim()}",
                    RequestDate = DateOnly.FromDateTime(now),
                    Status = "PENDING"
                };

                _context.RequestCancels.Add(requestCancel);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Gửi yêu cầu thành công. Dựa trên chính sách, bạn dự kiến được hoàn {refundPercentage}% giá trị tour.";
            }

            return RedirectToPage(new { filter = Filter });
        }
    }
}