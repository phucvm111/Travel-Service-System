using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public IndexModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public List<Tour> TopTour { get; set; } = new List<Tour>();
        public List<Feedback> RecentFeedbacks { get; set; } = new List<Feedback>();

        public async Task OnGetAsync()
        {
            TopTour = await _context.Tours
                .Where(t => t.Status == 1)
                .OrderByDescending(t => t.TourId)
                .Take(6)
                .ToListAsync();

            RecentFeedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .Where(f => f.Status == 1)
                .OrderByDescending(f => f.CreateDate)
                .Take(6)
                .ToListAsync();

            if (RecentFeedbacks == null)
            {
                RecentFeedbacks = new List<Feedback>();
            }

            if (TopTour == null)
            {
                TopTour = new List<Tour>();
            }
        }
    }
}