using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IHubContext<HubServer> _hub;

        public IndexModel(Prn222PrjContext context, IHubContext<HubServer> hub)
        {
            _context = context;
            _hub = hub;
        }

        public IList<Tour> Tour { get; set; } = new List<Tour>();
        public HashSet<int> BookedTourIds { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }
        private const int PageSize = 5;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Error");

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Status 1 (Sắp diễn ra) → 2 (Đang diễn ra)
            var startedTours = await _context.Tours
                .Where(t => t.TravelAgentId == agent.TravelAgentId
                         && t.Status == 1
                         && t.StartDay != null
                         && t.StartDay <= today)
                .ToListAsync();

            if (startedTours.Any())
            {
                startedTours.ForEach(t => t.Status = 2);
                await _context.SaveChangesAsync();
                try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }
            }

            // Status 2 (Đang diễn ra) → 3 (Hoàn thành)
            var expiredTours = await _context.Tours
                .Where(t => t.TravelAgentId == agent.TravelAgentId
                         && t.Status == 2
                         && t.EndDay != null
                         && t.EndDay < today)
                .ToListAsync();

            if (expiredTours.Any())
            {
                expiredTours.ForEach(t => t.Status = 3);
                await _context.SaveChangesAsync();
                try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }
            }

            var query = _context.Tours
                .Where(t => t.TravelAgentId == agent.TravelAgentId)
                .AsQueryable();

            if (Status != null)
                query = query.Where(t => t.Status == Status);

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            Tour = await query
                .OrderByDescending(t => t.TourId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var tourIds = Tour.Select(t => t.TourId).ToList();
            BookedTourIds = (await _context.BookDetails
                .Where(b => tourIds.Contains(b.TourId ?? 0))
                .Select(b => b.TourId!.Value)
                .Distinct()
                .ToListAsync()).ToHashSet();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId);

            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == id && t.TravelAgentId == agent!.TravelAgentId);

            if (tour == null) return NotFound();

            if (tour.Status != 1)
            {
                TempData["ErrorMessage"] = "Chỉ có thể xóa tour đang ở trạng thái Sắp diễn ra!";
                return RedirectToPage("./Index", new { Status, PageNumber });
            }

            bool hasBooking = await _context.BookDetails.AnyAsync(b => b.TourId == id);
            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Không thể xóa tour đã có khách đặt!";
                return RedirectToPage("./Index", new { Status, PageNumber });
            }

            var serviceDetails = _context.TourServiceDetails.Where(x => x.TourId == id);
            _context.TourServiceDetails.RemoveRange(serviceDetails);
            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            return RedirectToPage("./Index", new { Status, PageNumber });
        }
    }
}