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

        // Theo database mới, ID của các bảng này đều là ServiceId
        [BindProperty] public List<int> SelectedServices { get; set; } = new();

        public List<string> EndPlaces { get; set; } = new();
        public List<Accommodation> Hotels { get; set; } = new();
        public List<Restaurant> Restaurants { get; set; } = new();
        public List<Entertainment> Entertainments { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            Tour = tour;
            ExistingImage = tour.Image;

            // Lấy danh sách ServiceID đã có trong Tour_Service_Detail
            SelectedServices = await _context.TourServiceDetails
                .Where(td => td.TourId == id && td.ServiceId != null)
                .Select(td => td.ServiceId!.Value)
                .ToListAsync();

            LoadMasterData(userId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            if (ImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(_env.WebRootPath, "images/tour", fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
                Tour.Image = "images/tour/" + fileName;
            }
            else Tour.Image = ExistingImage;

            // Validate sơ bộ
            if (string.IsNullOrEmpty(Tour.TourName))
            {
                LoadMasterData(userId.Value);
                return Page();
            }

            var existingTour = await _context.Tours.FindAsync(Tour.TourId);
            if (existingTour == null) return NotFound();

            // Cập nhật thông tin chung
            _context.Entry(existingTour).CurrentValues.SetValues(Tour);

            // Cập nhật chi tiết dịch vụ
            var oldDetails = _context.TourServiceDetails.Where(d => d.TourId == Tour.TourId);
            _context.TourServiceDetails.RemoveRange(oldDetails);

            foreach (var sId in SelectedServices)
            {
                var service = await _context.Services.FindAsync(sId);
                string typeName = service?.ServiceType switch { 1 => "Accommodation", 2 => "Restaurant", 3 => "Entertainment", _ => "Service" };

                _context.TourServiceDetails.Add(new TourServiceDetail
                {
                    TourId = Tour.TourId,
                    ServiceId = sId,
                    ServiceName = typeName
                });
            }

            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("loadAll");

            return RedirectToPage("./Index");
        }

        private void LoadMasterData(int userId)
        {
            var agent = _context.TravelAgents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null) return;

            // Load kèm bảng Service để lấy Address và Name
            Hotels = _context.Accommodations.Include(a => a.Service)
                .Where(a => a.Service.AgentId == agent.TravelAgentId).ToList();

            Restaurants = _context.Restaurants.Include(r => r.Service)
                .Where(r => r.Service.AgentId == agent.TravelAgentId).ToList();

            Entertainments = _context.Entertainments.Include(e => e.Service)
                .Where(e => e.Service.AgentId == agent.TravelAgentId).ToList();

            // Lấy danh sách địa chỉ duy nhất từ tất cả dịch vụ của Agent này
            EndPlaces = _context.Services
                .Where(s => s.AgentId == agent.TravelAgentId && s.Address != null)
                .Select(s => s.Address!)
                .Distinct().ToList();
        }
    }
}