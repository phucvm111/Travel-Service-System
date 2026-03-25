using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    public class DetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public DetailsModel(FinalPrnContext context) => _context = context;

        public Tour Tour { get; set; }
        public List<TourDeparture> Departures { get; set; } = new();
        public List<Service> RestaurantList { get; set; } = new();
        public List<Service> EntertainmentList { get; set; } = new();
        public List<Service> AccommodationList { get; set; } = new();
        public List<Feedback> Feedbacks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (agent == null) return RedirectToPage("/Auths/Login");

            Tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == id && t.TravelAgentId == agent.TravelAgentId);
            if (Tour == null) return NotFound();

            Departures = await _context.TourDepartures
                .Where(d => d.TourId == id)
                .OrderBy(d => d.StartDate)
                .ToListAsync();

            var serviceIds = await _context.TourServiceDetails
                .Where(td => td.TourId == id)
                .Select(td => td.ServiceId)
                .ToListAsync();

            var allServices = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceId))
                .ToListAsync();

            RestaurantList = allServices.Where(s => s.ServiceType == 2).ToList();
            EntertainmentList = allServices.Where(s => s.ServiceType == 3).ToList();
            AccommodationList = allServices.Where(s => s.ServiceType == 1).ToList();

            Feedbacks = await _context.Feedbacks
                .Include(f => f.Book).ThenInclude(b => b.User)
                .Where(f => f.Book.TourDeparture.TourId == id)
                .OrderByDescending(f => f.CreateDate)
                .ToListAsync();

            return Page();
        }
    }
}