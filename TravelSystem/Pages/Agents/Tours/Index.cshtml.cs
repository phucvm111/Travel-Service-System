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

        // Search keyword
        [BindProperty(SupportsGet = true)]
        public string? SearchKeyword { get; set; }

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
            // Lógica này đã được chuyển sang BackgroundService: Services/TourDepartureStatusUpdaterService.cs


            // ── Query Tour ──
            var tourQuery = _context.Tours
                .Where(t => t.TravelAgentId == agent.TravelAgentId)
                .AsQueryable();

            // Lọc theo status Tour (1 = hoạt động, 0 = không hoạt động)
            if (TourStatus.HasValue)
                tourQuery = tourQuery.Where(t => t.Status == TourStatus);

            // Tìm kiếm theo tên tour, điểm đi, điểm đến
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var kw = SearchKeyword.Trim().ToLower();
                tourQuery = tourQuery.Where(t =>
                    t.TourName.ToLower().Contains(kw)
                    || t.StartPlace.ToLower().Contains(kw)
                    || t.EndPlace.ToLower().Contains(kw));
            }

            int totalItems = await tourQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            var pagedTours = await tourQuery
                .OrderByDescending(t => t.TourId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var tourIds = pagedTours.Select(t => t.TourId).ToList();

            // Load departures cho các tour trong trang hiện tại (tất cả)
            var departures = await _context.TourDepartures
                .Where(d => tourIds.Contains(d.TourId ?? 0))
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
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            var departureIds = tour.TourDepartures.Select(d => d.DepartureId).ToList();
            bool hasBooking = await _context.Bookings
                .AnyAsync(b => departureIds.Contains(b.TourDepartureId ?? 0));

            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Không thể xóa tour đã có khách đặt!";
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            var serviceDetails = _context.TourServiceDetails.Where(x => x.TourId == id);
            _context.TourServiceDetails.RemoveRange(serviceDetails);
            _context.TourDepartures.RemoveRange(tour.TourDepartures);
            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Xóa tour thành công!";
            return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
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
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            bool hasBooking = await _context.Bookings
                .AnyAsync(b => b.TourDepartureId == id);

            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Không thể xóa chuyến đi đã có khách đặt!";
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            _context.TourDepartures.Remove(departure);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Xóa chuyến đi thành công!";
            return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
        }

        // Thêm TourDeparture từ modal
        public async Task<IActionResult> OnPostCreateDepartureAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Auths/Login");

            int tourId = int.TryParse(Request.Form["NewDepTourId"], out var tid) ? tid : 0;
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.TourId == tourId && t.TravelAgentId == agent.TravelAgentId);
            if (tour == null) return NotFound();

            var startDateStr = Request.Form["NewDepStartDate"].ToString();
            var endDateStr = Request.Form["NewDepEndDate"].ToString();
            var adultRaw = Request.Form["NewDepAdultPriceRaw"].ToString().Replace(".", "").Replace(",", "");
            var childRaw = Request.Form["NewDepChildPriceRaw"].ToString().Replace(".", "").Replace(",", "");
            var capRaw = Request.Form["NewDepCapacityRaw"].ToString();

            var dep = new TourDeparture
            {
                TourId = tourId,
                StartDate = DateOnly.TryParse(startDateStr, out var sd) ? sd : null,
                EndDate = DateOnly.TryParse(endDateStr, out var ed) ? ed : null,
                AdultPrice = double.TryParse(adultRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ap) ? ap : 0,
                ChildPrice = double.TryParse(childRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cp) ? cp : 0,
                Capacity = int.TryParse(capRaw, out var cap) ? cap : 0,
                Status = "active"
            };
            dep.AvailableSeat = dep.Capacity;

            _context.TourDepartures.Add(dep);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Thêm chuyến đi thành công!";
            return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
        }

        // Chỉnh sửa TourDeparture từ modal
        public async Task<IActionResult> OnPostEditDepartureAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Auths/Login");

            int depId = int.TryParse(Request.Form["EditDepId"], out var did) ? did : 0;
            int tourId = int.TryParse(Request.Form["EditDepTourId"], out var tid) ? tid : 0;

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.TourId == tourId && t.TravelAgentId == agent.TravelAgentId);
            if (tour == null) return NotFound();

            var dep = await _context.TourDepartures.FirstOrDefaultAsync(d => d.DepartureId == depId && d.TourId == tourId);
            if (dep == null) return NotFound();

            if (dep.Status?.Trim() != "active")
            {
                TempData["ErrorMessage"] = "Chỉ có thể sửa chuyến đi ở trạng thái Sắp diễn ra!";
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            var adultRaw = Request.Form["EditDepAdultPriceRaw"].ToString().Replace(".", "").Replace(",", "");
            var childRaw = Request.Form["EditDepChildPriceRaw"].ToString().Replace(".", "").Replace(",", "");
            var capRaw = Request.Form["EditDepCapacityRaw"].ToString();

            dep.StartDate = DateOnly.TryParse(Request.Form["EditDepStartDate"], out var sd) ? sd : dep.StartDate;
            dep.EndDate = DateOnly.TryParse(Request.Form["EditDepEndDate"], out var ed) ? ed : dep.EndDate;
            dep.AdultPrice = double.TryParse(adultRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ap) ? ap : dep.AdultPrice;
            dep.ChildPrice = double.TryParse(childRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cp) ? cp : dep.ChildPrice;

            int newCap = int.TryParse(capRaw, out var c) ? c : dep.Capacity ?? 0;
            int oldCap = dep.Capacity ?? 0;
            int booked = oldCap - (dep.AvailableSeat ?? 0);
            dep.Capacity = newCap;
            dep.AvailableSeat = Math.Max(0, newCap - booked);

            await _context.SaveChangesAsync();
            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Cập nhật chuyến đi thành công!";
            return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
        }
    }

    public class TourWithDepartures
    {
        public Tour Tour { get; set; } = null!;
        public List<TourDeparture> Departures { get; set; } = new();
    }
}