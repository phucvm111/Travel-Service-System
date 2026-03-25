using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public IndexModel(FinalPrnContext context)
        {
            _context = context;
        }

        public class TourCardViewModel
        {
            public int TourId { get; set; }
            public string? TourName { get; set; }
            public string? Image { get; set; }
            public string? StartPlace { get; set; }
            public string? EndPlace { get; set; }
            public int NumberOfDay { get; set; }
            public DateOnly? NearestStartDate { get; set; }
            public int? AvailableSeat { get; set; }
            public double? AdultPrice { get; set; }
        }

        public class FeedbackViewModel
        {
            public float? Rate { get; set; }
            public string? Content { get; set; }
            public string? Image { get; set; }
            public User? User { get; set; }
        }

        public IList<TourCardViewModel> TopTour { get; set; } = new List<TourCardViewModel>();
        public IList<FeedbackViewModel> RecentFeedbacks { get; set; } = new List<FeedbackViewModel>();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Lấy 6 tour đang hoạt động có ít nhất 1 departure còn chỗ sắp tới
            var tours = await _context.Tours
                .Where(t => t.Status == 1
                         && t.TourDepartures.Any(d =>
                             d.Status != null
                             && d.Status.Trim() == "active"
                             && d.AvailableSeat > 0
                             && d.StartDate >= today))
                .OrderByDescending(t => t.TourId)
                .Take(6)
                .Include(t => t.TourDepartures)
                .ToListAsync();

            TopTour = tours.Select(t =>
            {
                // Lấy departure gần nhất còn chỗ
                var nearest = t.TourDepartures
                    .Where(d => d.Status != null
                             && d.Status.Trim() == "active"
                             && d.AvailableSeat > 0
                             && d.StartDate >= today)
                    .OrderBy(d => d.StartDate)
                    .FirstOrDefault();

                return new TourCardViewModel
                {
                    TourId = t.TourId,
                    TourName = t.TourName,
                    Image = t.Image,
                    StartPlace = t.StartPlace,
                    EndPlace = t.EndPlace,
                    NumberOfDay = t.NumberOfDay,
                    NearestStartDate = nearest?.StartDate,
                    AvailableSeat = nearest?.AvailableSeat,
                    AdultPrice = nearest?.AdultPrice,
                };
            }).ToList();

            // Lấy 3 feedback gần nhất
            RecentFeedbacks = await _context.Feedbacks
                .Where(f => f.Content != null)
                .OrderByDescending(f => f.CreateDate)
                .Take(3)
                .Select(f => new FeedbackViewModel
                {
                    Rate = (float?)f.Rate,
                    Content = f.Content,
                    Image = f.Image,
                    User = f.Book != null ? f.Book.User : null
                })
                .ToListAsync();
        }
    }
}
