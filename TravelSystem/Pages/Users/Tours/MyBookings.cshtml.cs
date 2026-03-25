using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TravelSystem.Pages.Users.BookTours
{
    public class MyBookingsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public MyBookingsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public List<BookDetail> BookingList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            const int pageSize = 5;

            var query = _context.BookDetails
                .Include(b => b.Tour)
                .Where(b => b.UserId == userId && (b.Status == 1 || b.Status == 5 || b.Status == 7))
                .AsQueryable();

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (PageNumber < 1) PageNumber = 1;
            if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

            BookingList = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int bookID, string reason)
        {
            var booking = await _context.BookDetails.FindAsync(bookID);
            if (booking == null) return RedirectToPage(new { cancelSuccess = "false" });

            _context.RequestCancels.Add(new RequestCancel
            {
                BookId = bookID,
                UserId = booking.UserId ?? 0,
                Reason = reason,
                RequestDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "PENDING"
            });

            booking.Status = 5; // Chờ hoàn tiền
            await _context.SaveChangesAsync();

            return RedirectToPage(new { cancelSuccess = "true" });
        }
    }
}