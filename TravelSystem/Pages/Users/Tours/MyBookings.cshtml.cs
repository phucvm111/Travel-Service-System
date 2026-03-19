using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.BookTours
{
    public class MyBookingsModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public MyBookingsModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public List<BookDetail> BookingList { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync(int page = 1, string type = "current")
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            const int pageSize = 5;
            CurrentPage = page;

            // Truy vấn dữ liệu nạp kèm thông tin Tour
            var query = _context.BookDetails
                .Include(b => b.Tour)
                .Where(b => b.UserId == userId);

            // Phân loại: Sắp tới (1, 5, 7) hoặc Đã xong (4, 6)
            if (type == "finished")
            {
                query = query.Where(b => b.Status == 4 || b.Status == 6);
            }
            else
            {
                query = query.Where(b => b.Status == 1 || b.Status == 5 || b.Status == 7);
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            BookingList = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["Type"] = type;
            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int bookID, string reason)
        {
            var booking = await _context.BookDetails.FindAsync(bookID);
            if (booking == null) return RedirectToPage(new { cancelSuccess = "false" });

            var cancelRequest = new RequestCancel
            {
                BookId = bookID,
                UserId = booking.UserId ?? 0,
                Reason = reason,
                RequestDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "PENDING"
            };

            booking.Status = 5;

            _context.RequestCancels.Add(cancelRequest);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { cancelSuccess = "true" });
        }
    }
}