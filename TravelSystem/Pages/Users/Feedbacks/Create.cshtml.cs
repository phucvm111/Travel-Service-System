using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Feedbacks
{
    public class CreateModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(FinalPrnContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Feedback Feedback { get; set; } = new();

        public Booking? Booking { get; set; }

        public Feedback? ExistingFeedback { get; set; }

        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        public string TourName => Booking?.TourDeparture?.Tour?.TourName ?? "Chuyến đi";

        public string BookDateText => Booking?.BookDate?.ToString("dd/MM/yyyy") ?? "";

        public async Task<IActionResult> OnGetAsync(int bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            Booking = await _context.Bookings
                .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.Tour)
                .FirstOrDefaultAsync(b => b.BookId == bookingId && b.UserId == userId.Value);

            if (Booking == null)
                return RedirectToPage("/Users/BookTours/MyBookings", new { type = "finished" });

            ExistingFeedback = await _context.Feedbacks
                .Include(f => f.Book)
                .FirstOrDefaultAsync(f => f.BookId == bookingId && f.Book.UserId == userId.Value);

            Feedback.BookId = bookingId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            Booking = await _context.Bookings
                .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.Tour)
                .FirstOrDefaultAsync(b => b.BookId == Feedback.BookId && b.UserId == userId.Value);

            if (Booking == null)
                return RedirectToPage("/Users/BookTours/MyBookings", new { type = "finished" });

            ExistingFeedback = await _context.Feedbacks
                .Include(f => f.Book)
                .FirstOrDefaultAsync(f => f.BookId == Feedback.BookId && f.Book.UserId == userId.Value);

            if (ExistingFeedback != null)
            {
                return Page();
            }

            if (!Feedback.Rate.HasValue || Feedback.Rate < 1 || Feedback.Rate > 5)
            {
                ModelState.AddModelError("Feedback.Rate", "Vui lòng chọn số sao từ 1 đến 5.");
            }

            if (string.IsNullOrWhiteSpace(Feedback.Content))
            {
                ModelState.AddModelError("Feedback.Content", "Vui lòng nhập nội dung phản hồi.");
            }
            else if (Feedback.Content.Trim().Length < 5)
            {
                ModelState.AddModelError("Feedback.Content", "Nội dung phản hồi phải có ít nhất 5 ký tự.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Feedback.BookId = Booking.BookId;
            Feedback.CreateDate = DateTime.Now;

            if (UploadImage != null && UploadImage.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "images", "feedback");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(UploadImage.FileName);
                var path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await UploadImage.CopyToAsync(stream);
                }

                Feedback.Image = "/images/feedback/" + fileName;
            }
            else
            {
                Feedback.Image = null;
            }

            _context.Feedbacks.Add(Feedback);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Users/BookTours/MyBookings", new { type = "finished" });
        }
    }
}