using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    public class CreateDepartureModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hub;

        public CreateDepartureModel(FinalPrnContext context, IHubContext<HubServer> hub)
        {
            _context = context;
            _hub = hub;
        }

        // ── Tour info (display only) ──
        public Tour Tour { get; set; } = new();

        // ── Departure form data ──
        public TourDeparture Departure { get; set; } = new();
        public string? AdultPriceRaw { get; set; }
        public string? ChildPriceRaw { get; set; }
        public string? CapacityRaw { get; set; }

        // ═══════════════════════════════════════════════════════
        //  GET
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> OnGetAsync(int tourId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Error");

            Tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == tourId && t.TravelAgentId == agent.TravelAgentId);
            if (Tour == null) return NotFound();

            return Page();
        }

        // ═══════════════════════════════════════════════════════
        //  POST
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Error");

            // Read tourId from hidden field
            int tourId = int.TryParse(Request.Form["TourId"], out var tid) ? tid : 0;

            Tour = await _context.Tours
                .FirstOrDefaultAsync(t => t.TourId == tourId && t.TravelAgentId == agent.TravelAgentId);
            if (Tour == null) return NotFound();

            ModelState.Clear();

            // Read departure data from form
            AdultPriceRaw = Request.Form["AdultPriceRaw"].ToString().Trim();
            ChildPriceRaw = Request.Form["ChildPriceRaw"].ToString().Trim();
            CapacityRaw = Request.Form["CapacityRaw"].ToString().Trim();

            Departure = new TourDeparture
            {
                StartDate = DateOnly.TryParse(Request.Form["Departure.StartDate"], out var sd) ? sd : null,
                EndDate = DateOnly.TryParse(Request.Form["Departure.EndDate"], out var ed) ? ed : null,
                AdultPrice = double.TryParse(
                    AdultPriceRaw.Replace(".", "").Replace(",", ""),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var ap) ? ap : 0,
                ChildPrice = double.TryParse(
                    ChildPriceRaw.Replace(".", "").Replace(",", ""),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var cp) ? cp : 0,
                Capacity = int.TryParse(CapacityRaw, out var cap) ? cap : 0,
            };

            // Validate
            ValidateDeparture();
            if (!ModelState.IsValid)
                return Page();

            // Save
            Departure.TourId = Tour.TourId;
            Departure.AvailableSeat = Departure.Capacity;
            Departure.Status = "active";
            _context.TourDepartures.Add(Departure);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["SuccessMessage"] = "Thêm chuyến đi thành công!";
            return RedirectToPage("./Index");
        }

        // ═══════════════════════════════════════════════════════
        //  Validate – same logic as Create.cshtml.cs
        // ═══════════════════════════════════════════════════════
        private void ValidateDeparture()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (Departure.StartDate == null)
                ModelState.AddModelError("Departure.StartDate", "Ngày khởi hành không được để trống!");
            else if (Departure.StartDate <= today)
                ModelState.AddModelError("Departure.StartDate", "Ngày khởi hành phải sau ngày hiện tại!");

            if (Departure.EndDate == null)
                ModelState.AddModelError("Departure.EndDate", "Ngày kết thúc không được để trống!");
            else if (Departure.StartDate != null && Departure.EndDate < Departure.StartDate)
                ModelState.AddModelError("Departure.EndDate", "Ngày kết thúc phải sau ngày khởi hành!");

            if (Departure.StartDate != null && Departure.EndDate != null)
            {
                var actualDays = (Departure.EndDate.Value.ToDateTime(TimeOnly.MinValue)
                                - Departure.StartDate.Value.ToDateTime(TimeOnly.MinValue)).Days + 1;
                if (actualDays != Tour.NumberOfDay)
                    ModelState.AddModelError("Departure.EndDate",
                        $"Số ngày của chuyến đi ({actualDays} ngày) không khớp với tour ({Tour.NumberOfDay} ngày)!");
            }

            if (string.IsNullOrWhiteSpace(AdultPriceRaw) || Departure.AdultPrice <= 0)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn phải lớn hơn 0!");
            else if (Departure.AdultPrice > 100_000_000)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn không được vượt quá 100 triệu đồng!");

            if (string.IsNullOrWhiteSpace(ChildPriceRaw) || Departure.ChildPrice < 0)
                ModelState.AddModelError("ChildPriceRaw", "Giá trẻ em không hợp lệ!");
            else if (Departure.ChildPrice > 100_000_000)
                ModelState.AddModelError("ChildPriceRaw", "Giá trẻ em không được vượt quá 100 triệu đồng!");

            if (!string.IsNullOrWhiteSpace(AdultPriceRaw) && !string.IsNullOrWhiteSpace(ChildPriceRaw)
                && Departure.AdultPrice > 0 && Departure.ChildPrice >= 0
                && Departure.AdultPrice <= Departure.ChildPrice)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn phải lớn hơn giá trẻ em!");

            if (string.IsNullOrWhiteSpace(CapacityRaw) || Departure.Capacity <= 0)
                ModelState.AddModelError("CapacityRaw", "Số chỗ phải lớn hơn 0!");
            else if (Departure.Capacity > 1000)
                ModelState.AddModelError("CapacityRaw", "Số chỗ không được vượt quá 1000!");
        }
    }
}
