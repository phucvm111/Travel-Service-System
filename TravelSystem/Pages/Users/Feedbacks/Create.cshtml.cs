using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Feedbacks
{
    public class CreateModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(Prn222PrjContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Feedback Feedback { get; set; } = new();

        public BookDetail? Booking { get; set; }

        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            Booking = await _context.BookDetails
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.BookId == bookingId && b.UserId == userId);

            if (Booking == null)
                return RedirectToPage("/Users/Tours/FinishedBookings");

            Feedback.BookId = bookingId;
            Feedback.UserId = userId.Value;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            Feedback.UserId = userId.Value;
            Feedback.CreateDate = DateTime.Now;
            Feedback.Status = 1;

            if (UploadImage != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "images", "feedback");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

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
                Feedback.Image = "/images/11.jpg";
            }

            _context.Feedbacks.Add(Feedback);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Users/Tours/FinishedBookings");
        }
    }
}