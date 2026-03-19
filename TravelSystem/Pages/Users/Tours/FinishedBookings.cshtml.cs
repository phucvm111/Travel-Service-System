using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.BookTours
{
    public class FinishedBookingsModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IWebHostEnvironment _environment;

        public FinishedBookingsModel(Prn222PrjContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public List<BookDetail> BookingList { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [TempData] public string SuccessMessage { get; set; }
        [TempData] public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            const int pageSize = 5;
            CurrentPage = page;

            var query = _context.BookDetails
                .Include(b => b.Tour)
                .Where(b => b.UserId == userId && (b.Status == 4 || b.Status == 6)); // 4: Hoàn thành, 6: Đã hoàn tiền

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            BookingList = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddFeedbackAsync(int bookID, int rate, string content, IFormFile image)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            try
            {
                var feedback = new Feedback
                {
                    BookId = bookID,
                    UserId = userId.Value,
                    Rate = rate,
                    Content = content,
                    CreateDate = DateTime.Now,
                    Status = 1
                };

                if (image != null)
                {
                    string folder = Path.Combine(_environment.WebRootPath, "images/feedbacks");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    feedback.Image = "images/feedbacks/" + fileName;
                }

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                SuccessMessage = "Gửi phản hồi thành công!";
            }
            catch (Exception)
            {
                ErrorMessage = "Có lỗi xảy ra khi gửi phản hồi.";
            }

            return RedirectToPage();
        }
    }
}