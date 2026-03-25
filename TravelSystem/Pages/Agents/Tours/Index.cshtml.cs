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
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hub;

        public IndexModel(FinalPrnContext context, IHubContext<HubServer> hub)
        {
            _context = context;
            _hub = hub;
        }

        public IList<TourWithDepartures> TourList { get; set; } = new List<TourWithDepartures>();
        public HashSet<int> BookedTourIds { get; set; } = new();

        // Filter theo status TourDeparture: "active" | "ongoing" | "completed" | null = tất cả
        [BindProperty(SupportsGet = true)]
        public string? DepartureStatus { get; set; }

        // Filter theo status Tour: 1 = hoạt động, 0 = không hoạt động, null = tất cả
        [BindProperty(SupportsGet = true)]
        public int? TourStatus { get; set; }

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

            // ── Tự động cập nhật status TourDeparture theo ngày ──

            // "active" → "ongoing" khi đã đến startDate
            var toOngoing = await _context.TourDepartures
                .Where(d => d.Tour!.TravelAgentId == agent.TravelAgentId
                         && d.Status != null && d.Status.Trim() == "active"
                         && d.StartDate != null
                         && d.StartDate <= today)
                .ToListAsync();

            if (toOngoing.Any())
            {
                toOngoing.ForEach(d => d.Status = "ongoing");
                await _context.SaveChangesAsync();
                try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }
            }

            // "ongoing" → "completed" khi đã qua endDate
            var toCompleted = await _context.TourDepartures
                .Where(d => d.Tour!.TravelAgentId == agent.TravelAgentId
                         && d.Status != null && d.Status.Trim() == "ongoing"
                         && d.EndDate != null
                         && d.EndDate < today)
                .ToListAsync();

            if (toCompleted.Any())
            {
                toCompleted.ForEach(d => d.Status = "completed");
                await _context.SaveChangesAsync();
                try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }
            }

            // ── Query Tour ──
            var tourQuery = _context.Tours
                .Where(t => t.TravelAgentId == agent.TravelAgentId)
                .AsQueryable();

            // Lọc theo status Tour (1 = hoạt động, 0 = không hoạt động)
            if (TourStatus.HasValue)
                tourQuery = tourQuery.Where(t => t.Status == TourStatus);

            // Lọc theo departure status → chỉ lấy tour có ít nhất 1 departure khớp
            if (!string.IsNullOrEmpty(DepartureStatus))
                tourQuery = tourQuery.Where(t =>
                    t.TourDepartures.Any(d => d.Status != null && d.Status.Trim() == DepartureStatus));

            int totalItems = await tourQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            var pagedTours = await tourQuery
                .OrderByDescending(t => t.TourId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var tourIds = pagedTours.Select(t => t.TourId).ToList();

            // Load departures cho các tour trong trang hiện tại
            var departureQuery = _context.TourDepartures
                .Where(d => tourIds.Contains(d.TourId ?? 0));

            if (!string.IsNullOrEmpty(DepartureStatus))
                departureQuery = departureQuery.Where(d =>
                    d.Status != null && d.Status.Trim() == DepartureStatus);

            var departures = await departureQuery
                .OrderBy(d => d.StartDate)
                .ToListAsync();

            TourList = pagedTours.Select(t => new TourWithDepartures
            {
                Tour = t,
                Departures = departures.Where(d => d.TourId == t.TourId).ToList()
            }).ToList();

            BookedTourIds = (await _context.Bookings
                .Where(b => b.TourDeparture != null
                         && tourIds.Contains(b.TourDeparture.TourId ?? 0))
                .Select(b => b.TourDeparture!.TourId!.Value)
                .Distinct()
                .ToListAsync()).ToHashSet();

            return Page();
        }

        // Xóa Tour
        public async Task<IActionResult> OnPostDeleteTourAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId);

            var tour = await _context.Tours
                .Include(t => t.TourDepartures)
                .FirstOrDefaultAsync(t => t.TourId == id && t.TravelAgentId == agent!.TravelAgentId);

            if (tour == null) return NotFound();

            // Không cho xóa nếu có departure đang diễn ra hoặc hoàn thành
            bool hasActiveDeparture = tour.TourDepartures
                .Any(d => d.Status?.Trim() == "ongoing" || d.Status?.Trim() == "completed");

            if (hasActiveDeparture)
            {
                TempData["ErrorMessage"] = "Không thể xóa tour đã có chuyến đang diễn ra hoặc đã hoàn thành!";
                return RedirectToPage("./Index", new { DepartureStatus, TourStatus, PageNumber });
            }

            var departureIds = tour.TourDepartures.Select(d => d.DepartureId).ToList();
            bool hasBooking = await _context.Bookings
                .AnyAsync(b => departureIds.Contains(b.TourDepartureId ?? 0));

            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Không thể xóa tour đã có khách đặt!";
                return RedirectToPage("./Index", new { DepartureStatus, TourStatus, PageNumber });
            }

            var serviceDetails = _context.TourServiceDetails.Where(x => x.TourId == id);
            _context.TourServiceDetails.RemoveRange(serviceDetails);
            _context.TourDepartures.RemoveRange(tour.TourDepartures);
            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Xóa tour thành công!";
            return RedirectToPage("./Index", new { DepartureStatus, TourStatus, PageNumber });
        }

        // Xóa TourDeparture
        public async Task<IActionResult> OnPostDeleteDepartureAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var departure = await _context.TourDepartures
                .Include(d => d.Tour)
                .FirstOrDefaultAsync(d => d.DepartureId == id);

            if (departure == null) return NotFound();

            if (departure.Status?.Trim() != "active")
            {
                TempData["ErrorMessage"] = "Chỉ có thể xóa chuyến đi ở trạng thái Sắp diễn ra!";
                return RedirectToPage("./Index", new { DepartureStatus, TourStatus, PageNumber });
            }

            bool hasBooking = await _context.Bookings
                .AnyAsync(b => b.TourDepartureId == id);

            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Không thể xóa chuyến đi đã có khách đặt!";
                return RedirectToPage("./Index", new { DepartureStatus, TourStatus, PageNumber });
            }

            _context.TourDepartures.Remove(departure);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Xóa chuyến đi thành công!";
            return RedirectToPage("./Index", new { DepartureStatus, TourStatus, PageNumber });
        }
    }

    public class TourWithDepartures
    {
        public Tour Tour { get; set; } = null!;
        public List<TourDeparture> Departures { get; set; } = new();
    }
}