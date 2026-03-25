using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class DetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DetailsModel(FinalPrnContext context)
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
            Tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (Tour == null)
            {
                return NotFound();
            }


            // Restaurant
            RestaurantList = await _context.Restaurants
                .Where(r => _context.TourServiceDetails
                    .Any(t => t.ServiceId == r.ServiceId && t.TourId == id))
                .ToListAsync();


            // Entertainment
            EntertainmentList = await _context.Entertainments
                .Where(e => _context.TourServiceDetails
                    .Any(t => t.ServiceId == e.ServiceId && t.TourId == id))
                .ToListAsync();


            // Accommodation
            AccommodationList = await _context.Accommodations
                .Where(a => _context.TourServiceDetails
                    .Any(t => t.ServiceId == a.ServiceId && t.TourId == id))
                .ToListAsync();


            // Feedback
            Feedbacks = await _context.Feedbacks
                .Where(f => f.Book.TourId == id)
                .OrderByDescending(f => f.CreateDate)
                .ToListAsync();

            return Page();
        }
    }
}