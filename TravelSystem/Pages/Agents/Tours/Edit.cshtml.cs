using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using Microsoft.AspNetCore.SignalR;
using TravelSystem.Hubs;

namespace TravelSystem.Pages.Agents.Tours
{
    public class EditModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<HubServer> _hub;

        public EditModel(FinalPrnContext context, IWebHostEnvironment env, IHubContext<HubServer> hub)
        {
            _context = context; _env = env; _hub = hub;
        }

        [BindProperty] public Tour Tour { get; set; } = new();
        [BindProperty] public IFormFile? ImageFile { get; set; }
        [BindProperty] public string? ExistingImage { get; set; }

        [BindProperty] public List<int> SelectedHotels { get; set; } = new();
        [BindProperty] public List<int> SelectedRestaurants { get; set; } = new();
        [BindProperty] public List<int> SelectedEntertainments { get; set; } = new();

        public List<string> EndPlaces { get; set; } = new();
        public List<ServiceItem> Hotels { get; set; } = new();
        public List<ServiceItem> Restaurants { get; set; } = new();
        public List<ServiceItem> Entertainments { get; set; } = new();

        // Lưu số ngày gốc để hiển thị readonly trên View
        public int OriginalNumberOfDay { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            Tour = tour;
            ExistingImage = tour.Image;
            OriginalNumberOfDay = tour.NumberOfDay;

            var existingDetails = await _context.TourServiceDetails
                .Where(td => td.TourId == id && td.ServiceId != null)
                .ToListAsync();

            SelectedHotels = existingDetails
                .Where(d => d.ServiceName == "Accommodation")
                .Select(d => d.ServiceId!.Value).ToList();
            SelectedRestaurants = existingDetails
                .Where(d => d.ServiceName == "Restaurant")
                .Select(d => d.ServiceId!.Value).ToList();
            SelectedEntertainments = existingDetails
                .Where(d => d.ServiceName == "Entertainment")
                .Select(d => d.ServiceId!.Value).ToList();

            LoadServices(userId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            // Upload ảnh mới nếu có
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "images", "tour");
                Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(folder, fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
                Tour.Image = "images/tour/" + fileName;
            }
            else
            {
                Tour.Image = ExistingImage;
            }

            if (string.IsNullOrWhiteSpace(Tour.TourName) ||
                string.IsNullOrWhiteSpace(Tour.StartPlace) ||
                string.IsNullOrWhiteSpace(Tour.EndPlace))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ Tên tour, Điểm bắt đầu và Điểm đến.");
                LoadServices(userId.Value);
                return Page();
            }

            var existingTour = await _context.Tours.FindAsync(Tour.TourId);
            if (existingTour == null) return NotFound();

            // Lưu lại để hiển thị readonly đúng khi có lỗi
            OriginalNumberOfDay = existingTour.NumberOfDay;

            existingTour.TourName = Tour.TourName;
            existingTour.StartPlace = Tour.StartPlace;
            existingTour.EndPlace = Tour.EndPlace;
            existingTour.Image = Tour.Image;
            existingTour.TourIntroduce = Tour.TourIntroduce;
            existingTour.TourSchedule = Tour.TourSchedule;
            existingTour.TourInclude = Tour.TourInclude;
            existingTour.TourNonInclude = Tour.TourNonInclude;
            // ✅ KHÔNG cập nhật NumberOfDay — giữ nguyên giá trị gốc trong DB

            var oldDetails = _context.TourServiceDetails.Where(d => d.TourId == Tour.TourId);
            _context.TourServiceDetails.RemoveRange(oldDetails);

            foreach (var sId in SelectedHotels.Where(x => x > 0))
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = Tour.TourId, ServiceId = sId, ServiceName = "Accommodation" });

            foreach (var sId in SelectedRestaurants.Where(x => x > 0))
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = Tour.TourId, ServiceId = sId, ServiceName = "Restaurant" });

            foreach (var sId in SelectedEntertainments.Where(x => x > 0))
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = Tour.TourId, ServiceId = sId, ServiceName = "Entertainment" });

            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("loadAll");

            TempData["EditSuccess"] = "1";
            return RedirectToPage("./Index");
        }

        private void LoadServices(int userId)
        {
            var agent = _context.TravelAgents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null) return;

            var hotelSids = _context.Accommodations.Select(a => a.ServiceId).ToHashSet();
            var restaurantSids = _context.Restaurants.Select(r => r.ServiceId).ToHashSet();
            var entertainmentSids = _context.Entertainments.Select(e => e.ServiceId).ToHashSet();

            var allServices = _context.Services
                .Where(s => s.AgentId == agent.TravelAgentId && s.Status == 1)
                .Select(s => new ServiceItem
                {
                    ServiceId = s.ServiceId,
                    Name = s.ServiceName ?? "",
                    Address = s.Address ?? ""
                })
                .ToList();

            Hotels = allServices.Where(s => hotelSids.Contains(s.ServiceId)).ToList();
            Restaurants = allServices.Where(s => restaurantSids.Contains(s.ServiceId)).ToList();
            Entertainments = allServices.Where(s => entertainmentSids.Contains(s.ServiceId)).ToList();

            EndPlaces = allServices
                .Select(s => s.Address)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();

            if (!string.IsNullOrWhiteSpace(Tour?.EndPlace) && !EndPlaces.Contains(Tour.EndPlace))
                EndPlaces.Add(Tour.EndPlace);
        }
    }
}
