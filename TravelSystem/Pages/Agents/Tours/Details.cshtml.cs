using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    public class DetailsModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public DetailsModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public Tour Tour { get; set; }
        public List<Restaurant> RestaurantList { get; set; } = new();
        public List<Entertainment> EntertainmentList { get; set; } = new();
        public List<Accommodation> AccommodationList { get; set; } = new();
        public List<Feedback> Feedbacks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Chỉ Agent đã đăng nhập mới vào được
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (agent == null) return RedirectToPage("/Auths/Login");

            // Chỉ cho xem tour của chính agent đó
            Tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == id && t.TravelAgentId == agent.TravelAgentId);

            if (Tour == null) return NotFound();

            // Nhà hàng
            RestaurantList = await _context.Restaurants
                .Where(r => _context.TourServiceDetails
                    .Any(t => t.ServiceId == r.ServiceId && t.TourId == id))
                .ToListAsync();

            // Giải trí
            EntertainmentList = await _context.Entertainments
                .Where(e => _context.TourServiceDetails
                    .Any(t => t.ServiceId == e.ServiceId && t.TourId == id))
                .ToListAsync();

            // Khách sạn
            AccommodationList = await _context.Accommodations
                .Where(a => _context.TourServiceDetails
                    .Any(t => t.ServiceId == a.ServiceId && t.TourId == id))
                .ToListAsync();

            // Feedback
            Feedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .Where(f => f.Book.TourId == id)
                .OrderByDescending(f => f.CreateDate)
                .ToListAsync();

            return Page();
        }
    }
}
