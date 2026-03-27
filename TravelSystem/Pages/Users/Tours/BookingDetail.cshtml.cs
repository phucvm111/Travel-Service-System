using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class BookingDetailModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public BookingDetailModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Booking BookingItem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Kiểm tra đăng nhập
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            // Lấy chi tiết đơn đặt hàng bao gồm Tour và TravelAgent
            BookingItem = await _context.Bookings
                .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.Tour)
                        .ThenInclude(t => t.TravelAgent)
                .Include(b => b.PaymentMethod)
                .FirstOrDefaultAsync(m => m.BookId == id && m.UserId == userId);

            if (BookingItem == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}