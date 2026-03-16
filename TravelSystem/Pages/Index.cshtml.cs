using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public List<Tour> TopTour { get; set; }
        public List<Feedback> RecentFeedbacks { get; set; }

        public void OnGet()
        {
            TopTour = _context.Tours
                .OrderByDescending(t => t.TourId)
                .Take(6)
                .ToList();

            RecentFeedbacks = _context.Feedbacks
                .OrderByDescending(f => f.FeedbackId)
                .Take(3)
                .ToList();
        }
    }
}