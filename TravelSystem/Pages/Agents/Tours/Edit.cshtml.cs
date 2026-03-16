using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    public class EditModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<HubServer> _hub;

        public EditModel(Prn222PrjContext context, IWebHostEnvironment env, IHubContext<HubServer> hub)
        {
            _context = context;
            _env = env;
            _hub = hub;
        }

        [BindProperty] public Tour Tour { get; set; } = new();
        [BindProperty] public IFormFile? ImageFile { get; set; }
        [BindProperty] public string? ExistingImage { get; set; }
        [BindProperty] public string? AdultPriceRaw { get; set; }
        [BindProperty] public string? ChildrenPriceRaw { get; set; }
        [BindProperty] public string? QuantityRaw { get; set; }

        public List<string> EndPlaces { get; set; } = new();
        public List<Accommodation> Hotels { get; set; } = new();
        public List<Restaurant> Restaurants { get; set; } = new();
        public List<Entertainment> Entertainments { get; set; } = new();

        [BindProperty] public List<int> SelectedHotels { get; set; } = new();
        [BindProperty] public List<int> SelectedRestaurants { get; set; } = new();
        [BindProperty] public List<int> SelectedEntertainments { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (agent == null || tour.TravelAgentId != agent.TravelAgentId)
                return Forbid();

            Tour = tour;
            ExistingImage = tour.Image;
            AdultPriceRaw = ((long)(tour.AdultPrice ?? 0)).ToString();
            ChildrenPriceRaw = ((long)(tour.ChildrenPrice ?? 0)).ToString();
            QuantityRaw = tour.Quantity?.ToString();

            var existingDetails = await _context.TourServiceDetails
                .Where(x => x.TourId == id).ToListAsync();

            var allServiceIds = existingDetails
                .Where(x => x.ServiceId != null)
                .Select(x => x.ServiceId!.Value).ToList();

            SelectedHotels = await _context.Accommodations
                .Where(h => allServiceIds.Contains(h.ServiceId))
                .Select(h => h.AccommodationId).ToListAsync();

            SelectedRestaurants = await _context.Restaurants
                .Where(r => allServiceIds.Contains(r.ServiceId))
                .Select(r => r.RestaurantId).ToListAsync();

            SelectedEntertainments = await _context.Entertainments
                .Where(e => allServiceIds.Contains(e.ServiceId))
                .Select(e => e.EntertainmentId).ToListAsync();

            LoadServices(userId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            LoadServices(userId.Value);
            ModelState.Clear();

            SelectedHotels ??= new List<int>();
            SelectedRestaurants ??= new List<int>();
            SelectedEntertainments ??= new List<int>();

            Tour.AdultPrice = double.TryParse(AdultPriceRaw?.Replace(".", "").Replace(",", ""), out double ap) ? ap : 0;
            Tour.ChildrenPrice = double.TryParse(ChildrenPriceRaw?.Replace(".", "").Replace(",", ""), out double cp) ? cp : 0;
            Tour.Quantity = int.TryParse(QuantityRaw, out int qty) ? qty : 0;

            if (ImageFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "images/tour");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
                Tour.Image = "images/tour/" + fileName;
                ExistingImage = Tour.Image;
            }
            else if (!string.IsNullOrWhiteSpace(ExistingImage))
            {
                Tour.Image = ExistingImage;
            }

            ValidateInput();
            if (!ModelState.IsValid) return Page();

            var existing = await _context.Tours.FindAsync(Tour.TourId);
            if (existing == null) return NotFound();

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (agent == null || existing.TravelAgentId != agent.TravelAgentId)
                return Forbid();

            existing.TourName = Tour.TourName;
            existing.StartPlace = Tour.StartPlace;
            existing.EndPlace = Tour.EndPlace;
            existing.StartDay = Tour.StartDay;
            existing.EndDay = Tour.EndDay;
            existing.AdultPrice = Tour.AdultPrice;
            existing.ChildrenPrice = Tour.ChildrenPrice;
            existing.Quantity = Tour.Quantity;
            existing.TourIntroduce = Tour.TourIntroduce;
            existing.TourSchedule = Tour.TourSchedule;
            existing.TourInclude = Tour.TourInclude;
            existing.TourNonInclude = Tour.TourNonInclude;
            existing.Image = Tour.Image;
            existing.NumberOfDay =
                (existing.EndDay!.Value.ToDateTime(TimeOnly.MinValue)
                - existing.StartDay!.Value.ToDateTime(TimeOnly.MinValue)).Days + 1;

            await _context.SaveChangesAsync();

            var oldServices = _context.TourServiceDetails.Where(x => x.TourId == existing.TourId);
            _context.TourServiceDetails.RemoveRange(oldServices);
            await _context.SaveChangesAsync();

            await SaveServices(existing.TourId);

            // SignalR: không await để tránh bị block bởi client reload
            _ = _hub.Clients.All.SendAsync("loadAll");

            TempData["EditSuccess"] = true;
            return RedirectToPage("./Index");
        }

        private async Task SaveServices(int tourId)
        {
            foreach (var id in SelectedHotels.Where(x => x > 0))
            {
                var sid = _context.Accommodations
                    .Where(a => a.AccommodationId == id)
                    .Select(a => a.ServiceId).FirstOrDefault();
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = tourId, ServiceId = sid, ServiceName = "Accommodation" });
            }
            foreach (var id in SelectedRestaurants.Where(x => x > 0))
            {
                var sid = _context.Restaurants
                    .Where(r => r.RestaurantId == id)
                    .Select(r => r.ServiceId).FirstOrDefault();
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = tourId, ServiceId = sid, ServiceName = "Restaurant" });
            }
            foreach (var id in SelectedEntertainments.Where(x => x > 0))
            {
                var sid = _context.Entertainments
                    .Where(e => e.EntertainmentId == id)
                    .Select(e => e.ServiceId).FirstOrDefault();
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = tourId, ServiceId = sid, ServiceName = "Entertainment" });
            }
            await _context.SaveChangesAsync();
        }

        private void LoadServices(int userId)
        {
            var agent = _context.TravelAgents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null) return;

            var serviceIds = _context.Services
                .Where(s => s.TravelAgentId == agent.TravelAgentId)
                .Select(s => s.ServiceId).ToList();

            Hotels = _context.Accommodations.Where(h => serviceIds.Contains(h.ServiceId)).ToList();
            Restaurants = _context.Restaurants.Where(r => serviceIds.Contains(r.ServiceId)).ToList();
            Entertainments = _context.Entertainments.Where(e => serviceIds.Contains(e.ServiceId)).ToList();

            EndPlaces = Hotels.Select(h => h.Address)
                .Union(Restaurants.Select(r => r.Address))
                .Union(Entertainments.Select(e => e.Address))
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct().ToList();
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Tour.TourName))
                ModelState.AddModelError("Tour.TourName", "Tên tour không được để trống");
            else if (Tour.TourName.Trim().Length < 2 || Tour.TourName.Trim().Length > 255)
                ModelState.AddModelError("Tour.TourName", "Tên tour phải từ 2 đến 255 ký tự");

            if (string.IsNullOrWhiteSpace(Tour.StartPlace))
                ModelState.AddModelError("Tour.StartPlace", "Điểm bắt đầu không được để trống");

            if (string.IsNullOrWhiteSpace(Tour.EndPlace))
                ModelState.AddModelError("Tour.EndPlace", "Điểm đến không được để trống");

            if (Tour.StartDay == null)
                ModelState.AddModelError("Tour.StartDay", "Ngày bắt đầu không được để trống");
            else if (Tour.StartDay.Value <= DateOnly.FromDateTime(DateTime.Today))
                ModelState.AddModelError("Tour.StartDay", "Ngày bắt đầu phải sau ngày hiện tại");

            if (Tour.EndDay == null)
                ModelState.AddModelError("Tour.EndDay", "Ngày kết thúc không được để trống");
            else if (Tour.StartDay != null && Tour.EndDay < Tour.StartDay)
                ModelState.AddModelError("Tour.EndDay", "Ngày kết thúc phải sau ngày bắt đầu");

            if (string.IsNullOrWhiteSpace(AdultPriceRaw) || Tour.AdultPrice <= 0)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn phải lớn hơn 0");
            else if (Tour.AdultPrice > 100_000_000)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn không được vượt quá 100 triệu đồng");

            if (string.IsNullOrWhiteSpace(ChildrenPriceRaw) || Tour.ChildrenPrice < 0)
                ModelState.AddModelError("ChildrenPriceRaw", "Giá trẻ em không hợp lệ");
            else if (Tour.ChildrenPrice > 100_000_000)
                ModelState.AddModelError("ChildrenPriceRaw", "Giá trẻ em không được vượt quá 100 triệu đồng");

            if (!string.IsNullOrWhiteSpace(AdultPriceRaw) && !string.IsNullOrWhiteSpace(ChildrenPriceRaw)
                && Tour.AdultPrice > 0 && Tour.ChildrenPrice >= 0
                && Tour.AdultPrice <= Tour.ChildrenPrice)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn phải lớn hơn giá trẻ em");

            if (string.IsNullOrWhiteSpace(QuantityRaw) || Tour.Quantity <= 0)
                ModelState.AddModelError("QuantityRaw", "Số lượng phải lớn hơn 0");
            else if (Tour.Quantity > 1000)
                ModelState.AddModelError("QuantityRaw", "Số lượng không được vượt quá 1000");

            if (string.IsNullOrWhiteSpace(Tour.TourIntroduce))
                ModelState.AddModelError("Tour.TourIntroduce", "Giới thiệu không được để trống");
            else if (Tour.TourIntroduce.Trim().Length < 10 || Tour.TourIntroduce.Trim().Length > 3000)
                ModelState.AddModelError("Tour.TourIntroduce", "Giới thiệu phải từ 10 đến 3000 ký tự");

            if (string.IsNullOrWhiteSpace(Tour.TourSchedule))
                ModelState.AddModelError("Tour.TourSchedule", "Lịch trình không được để trống");
            else if (Tour.TourSchedule.Trim().Length < 10 || Tour.TourSchedule.Trim().Length > 5000)
                ModelState.AddModelError("Tour.TourSchedule", "Lịch trình phải từ 10 đến 5000 ký tự");

            if (string.IsNullOrWhiteSpace(Tour.TourInclude))
                ModelState.AddModelError("Tour.TourInclude", "Mục bao gồm không được để trống");
            else if (Tour.TourInclude.Trim().Length < 10 || Tour.TourInclude.Trim().Length > 2000)
                ModelState.AddModelError("Tour.TourInclude", "Mục bao gồm phải từ 10 đến 2000 ký tự");

            if (string.IsNullOrWhiteSpace(Tour.TourNonInclude))
                ModelState.AddModelError("Tour.TourNonInclude", "Mục không bao gồm không được để trống");
            else if (Tour.TourNonInclude.Trim().Length < 10 || Tour.TourNonInclude.Trim().Length > 2000)
                ModelState.AddModelError("Tour.TourNonInclude", "Mục không bao gồm phải từ 10 đến 2000 ký tự");

            if (ImageFile == null && string.IsNullOrWhiteSpace(ExistingImage))
                ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh cho tour");
        }
    }
}
