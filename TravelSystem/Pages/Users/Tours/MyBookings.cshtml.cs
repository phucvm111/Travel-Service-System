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
            if (userId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            var query = _context.Bookings
                .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.Tour)
                .Include(b => b.Feedbacks)
                .Include(b => b.RequestCancels)
                .Where(b => b.UserId == userId);

            if (Filter == "finished")
            {
                query = query.Where(b => b.Status == 4);
            }
            else
            {
                query = query.Where(b => b.Status != 4);
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (TotalPages > 0 && CurrentPage > TotalPages) CurrentPage = TotalPages;

            BookingList = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

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
            if (userId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookId == bookId && b.UserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToPage(new { filter = Filter });
            }

            if (booking.Status != 1 && booking.Status != 7)
            {
                TempData["Error"] = "Đơn hàng này không thể hủy.";
                return RedirectToPage(new { filter = Filter });
            }

            var requestCancel = new RequestCancel
            {
                BookId = bookId,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Khách hàng muốn hủy chuyến đi." : reason.Trim(),
                RequestDate = DateOnly.FromDateTime(DateTime.Now), // Fix: Convert DateTime to DateOnly
            };

            _context.RequestCancels.Add(requestCancel);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gửi yêu cầu hủy thành công.";
            return RedirectToPage(new { filter = Filter });
        }
    }
}