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

        [BindProperty(SupportsGet = true)]
        public string? SearchKeyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TourStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }
        private const int PageSize = 5;

        // ═══════════════════════════════════════════════════
        //  GET
        // ═══════════════════════════════════════════════════
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Error");

            var tourQuery = _context.Tours
                .Where(t => t.TravelAgentId == agent.TravelAgentId)
                .AsQueryable();

            if (TourStatus.HasValue)
                tourQuery = tourQuery.Where(t => t.Status == TourStatus);

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
                .Where(b => b.TourDeparture != null && tourIds.Contains(b.TourDeparture.TourId ?? 0))
                .Select(b => b.TourDeparture!.TourId!.Value)
                .Distinct()
                .ToListAsync()).ToHashSet();

            return Page();
        }

        // ═══════════════════════════════════════════════════
        //  XÓA TOUR
        // ═══════════════════════════════════════════════════
        public async Task<IActionResult> OnPostDeleteTourAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId);
            var tour = await _context.Tours
                .Include(t => t.TourDepartures)
                .FirstOrDefaultAsync(t => t.TourId == id && t.TravelAgentId == agent!.TravelAgentId);

            if (tour == null) return NotFound();

            bool hasActiveDeparture = tour.TourDepartures
                .Any(d => d.Status?.Trim() == "ongoing" || d.Status?.Trim() == "completed");

            if (hasActiveDeparture)
            {
                TempData["ErrorMessage"] = "Không thể xóa tour đã có chuyến đang diễn ra hoặc đã hoàn thành!";
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            var departureIds = tour.TourDepartures.Select(d => d.DepartureId).ToList();
            bool hasBooking = await _context.Bookings.AnyAsync(b => departureIds.Contains(b.TourDepartureId ?? 0));

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

        // ═══════════════════════════════════════════════════
        //  XÓA DEPARTURE
        // ═══════════════════════════════════════════════════
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
                TempData["OpenModalTourId"] = departure.TourId?.ToString();
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            bool hasBooking = await _context.Bookings.AnyAsync(b => b.TourDepartureId == id);
            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Không thể xóa chuyến đi đã có khách đặt!";
                TempData["OpenModalTourId"] = departure.TourId?.ToString();
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            int tourId = departure.TourId ?? 0;
            _context.TourDepartures.Remove(departure);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Xóa chuyến đi thành công!";
            TempData["OpenModalTourId"] = tourId.ToString();
            return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
        }

        // ═══════════════════════════════════════════════════
        //  THÊM DEPARTURE
        //  Validate đã chuyển sang client-side (JS).
        //  Server chỉ còn validate lần cuối để bảo vệ dữ liệu.
        // ═══════════════════════════════════════════════════
        public async Task<IActionResult> OnPostCreateDepartureAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Auths/Login");

            int tourId = int.TryParse(Request.Form["NewDepTourId"], out var tid) ? tid : 0;
            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == tourId && t.TravelAgentId == agent.TravelAgentId);
            if (tour == null) return NotFound();

            var startDateStr = Request.Form["NewDepStartDate"].ToString().Trim();
            var endDateStr = Request.Form["NewDepEndDate"].ToString().Trim();
            var adultRaw = Request.Form["NewDepAdultPriceRaw"].ToString().Replace(".", "").Replace(",", "").Trim();
            var childRaw = Request.Form["NewDepChildPriceRaw"].ToString().Replace(".", "").Replace(",", "").Trim();
            var capRaw = Request.Form["NewDepCapacityRaw"].ToString().Trim();

            DateOnly? startDate = DateOnly.TryParse(startDateStr, out var sd) ? sd : null;
            DateOnly? endDate = DateOnly.TryParse(endDateStr, out var ed) ? ed : null;
            double adultPrice = double.TryParse(adultRaw, System.Globalization.NumberStyles.Any,
                                       System.Globalization.CultureInfo.InvariantCulture, out var ap) ? ap : 0;
            double childPrice = double.TryParse(childRaw, System.Globalization.NumberStyles.Any,
                                       System.Globalization.CultureInfo.InvariantCulture, out var cp) ? cp : 0;
            int capacity = int.TryParse(capRaw, out var cap) ? cap : 0;

            // Server-side validate (bảo vệ dữ liệu — client đã validate trước)
            var errors = ValidateDeparture(startDate, endDate, adultRaw, adultPrice,
                                           childRaw, childPrice, capRaw, capacity, tour.NumberOfDay);
            if (errors.Any())
            {
                // Trường hợp hiếm (bypass JS) — trả về lỗi tổng qua ErrorMessage
                TempData["ErrorMessage"] = string.Join(" | ", errors);
                TempData["OpenModalTourId"] = tourId.ToString();
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            var dep = new TourDeparture
            {
                TourId = tourId,
                StartDate = startDate,
                EndDate = endDate,
                AdultPrice = adultPrice,
                ChildPrice = childPrice,
                Capacity = capacity,
                AvailableSeat = capacity,
                Status = "active"
            };

            _context.TourDepartures.Add(dep);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Thêm chuyến đi thành công!";
            TempData["OpenModalTourId"] = tourId.ToString();
            return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
        }

        // ═══════════════════════════════════════════════════
        //  SỬA DEPARTURE (có validate server-side + inline lỗi)
        // ═══════════════════════════════════════════════════
        public async Task<IActionResult> OnPostEditDepartureAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Auths/Login");

            int depId = int.TryParse(Request.Form["EditDepId"], out var did) ? did : 0;
            int tourId = int.TryParse(Request.Form["EditDepTourId"], out var tid) ? tid : 0;

            var tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == tourId && t.TravelAgentId == agent.TravelAgentId);
            if (tour == null) return NotFound();

            var dep = await _context.TourDepartures
                .FirstOrDefaultAsync(d => d.DepartureId == depId && d.TourId == tourId);
            if (dep == null) return NotFound();

            if (dep.Status?.Trim() != "active")
            {
                TempData["ErrorMessage"] = "Chỉ có thể sửa chuyến đi ở trạng thái Sắp diễn ra!";
                TempData["OpenModalTourId"] = tourId.ToString();
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            var startDateStr = Request.Form["EditDepStartDate"].ToString().Trim();
            var endDateStr = Request.Form["EditDepEndDate"].ToString().Trim();
            var adultRaw = Request.Form["EditDepAdultPriceRaw"].ToString().Replace(".", "").Replace(",", "").Trim();
            var childRaw = Request.Form["EditDepChildPriceRaw"].ToString().Replace(".", "").Replace(",", "").Trim();
            var capRaw = Request.Form["EditDepCapacityRaw"].ToString().Trim();

            DateOnly? startDate = DateOnly.TryParse(startDateStr, out var sd) ? sd : null;
            DateOnly? endDate = DateOnly.TryParse(endDateStr, out var ed) ? ed : null;
            double adultPrice = double.TryParse(adultRaw, System.Globalization.NumberStyles.Any,
                                      System.Globalization.CultureInfo.InvariantCulture, out var ap) ? ap : 0;
            double childPrice = double.TryParse(childRaw, System.Globalization.NumberStyles.Any,
                                      System.Globalization.CultureInfo.InvariantCulture, out var cp) ? cp : 0;
            int newCap = int.TryParse(capRaw, out var c) ? c : 0;

            var errors = ValidateDeparture(startDate, endDate, adultRaw, adultPrice,
                                           childRaw, childPrice, capRaw, newCap, tour.NumberOfDay);
            if (errors.Any())
            {
                // Trường hợp hiếm (bypass JS) — trả về lỗi tổng qua ErrorMessage
                TempData["ErrorMessage"] = string.Join(" | ", errors);
                TempData["OpenModalTourId"] = tourId.ToString();
                return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
            }

            int booked = (dep.Capacity ?? 0) - (dep.AvailableSeat ?? 0);
            dep.StartDate = startDate;
            dep.EndDate = endDate;
            dep.AdultPrice = adultPrice;
            dep.ChildPrice = childPrice;
            dep.Capacity = newCap;
            dep.AvailableSeat = Math.Max(0, newCap - booked);

            await _context.SaveChangesAsync();
            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Cập nhật chuyến đi thành công!";
            TempData["OpenModalTourId"] = tourId.ToString();
            return RedirectToPage("./Index", new { SearchKeyword, TourStatus, PageNumber });
        }

        // ═══════════════════════════════════════════════════
        //  VALIDATE DEPARTURE (dùng chung cho edit + fallback add)
        // ═══════════════════════════════════════════════════
        private static List<string> ValidateDeparture(
            DateOnly? startDate, DateOnly? endDate,
            string adultRaw, double adultPrice,
            string childRaw, double childPrice,
            string capRaw, int capacity,
            int? numberOfDay)
        {
            var errors = new List<string>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (startDate == null)
                errors.Add("Ngày khởi hành không được để trống!");
            else if (startDate <= today)
                errors.Add("Ngày khởi hành phải sau ngày hiện tại!");

            if (endDate == null)
                errors.Add("Ngày kết thúc không được để trống!");
            else if (startDate != null && endDate < startDate)
                errors.Add("Ngày kết thúc phải sau ngày khởi hành!");

            if (startDate != null && endDate != null && numberOfDay.HasValue)
            {
                var actualDays = (endDate.Value.ToDateTime(TimeOnly.MinValue)
                                - startDate.Value.ToDateTime(TimeOnly.MinValue)).Days + 1;
                if (actualDays != numberOfDay.Value)
                    errors.Add($"Số ngày của chuyến đi ({actualDays} ngày) không khớp với tour ({numberOfDay.Value} ngày)!");
            }

            if (string.IsNullOrWhiteSpace(adultRaw) || adultPrice <= 0)
                errors.Add("Giá người lớn phải lớn hơn 0!");
            else if (adultPrice > 100_000_000)
                errors.Add("Giá người lớn không được vượt quá 100 triệu đồng!");

            if (string.IsNullOrWhiteSpace(childRaw) || childPrice < 0)
                errors.Add("Giá trẻ em không hợp lệ!");
            else if (childPrice > 100_000_000)
                errors.Add("Giá trẻ em không được vượt quá 100 triệu đồng!");

            if (!string.IsNullOrWhiteSpace(adultRaw) && !string.IsNullOrWhiteSpace(childRaw)
                && adultPrice > 0 && childPrice >= 0 && adultPrice <= childPrice)
                errors.Add("Giá người lớn phải lớn hơn giá trẻ em!");

            if (string.IsNullOrWhiteSpace(capRaw) || capacity <= 0)
                errors.Add("Số chỗ phải lớn hơn 0!");
            else if (capacity > 1000)
                errors.Add("Số chỗ không được vượt quá 1000!");

            return errors;
        }
    }

    public class TourWithDepartures
    {
        public Tour Tour { get; set; } = null!;
        public List<TourDeparture> Departures { get; set; } = new();
    }
}
