using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.BookTours
{
    public class BookingDetailModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public BookingDetailModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public BookDetail Booking { get; set; }
        public Tour Tour { get; set; }
        public TravelAgent TravelAgent { get; set; }

        public async Task<IActionResult> OnGetAsync(int bookID)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            Booking = await _context.BookDetails
                .Include(b => b.Tour)
                    .ThenInclude(t => t.TravelAgent)
                .FirstOrDefaultAsync(b => b.BookId == bookID);

            if (Booking == null)
            {
                return NotFound();
            }

            Tour = Booking.Tour;
            TravelAgent = Tour?.TravelAgent;

            return Page();
        }
    }
}