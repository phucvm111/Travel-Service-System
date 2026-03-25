using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    // ViewModel hiển thị dịch vụ trong select
    // ServiceId = PK chung (= serviceID trong Accommodations / Restaurant / Entertainment)
    public class ServiceItem
    {
        public int ServiceId { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
    }

    public class CreateModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<HubServer> _hub;

        public CreateModel(FinalPrnContext context, IWebHostEnvironment env, IHubContext<HubServer> hub)
        {
            _context = context;
            _env = env;
            _hub = hub;
        }

        // ── Dữ liệu Tour ──────────────────────────────────────────────────────
        [BindNever] public Tour Tour { get; set; } = new();
        [BindProperty] public IFormFile? ImageFile { get; set; }
        [BindNever] public string? ExistingImage { get; set; }

        // ── Dữ liệu Departure ────────────────────────────────────────────────
        [BindNever] public string? AdultPriceRaw { get; set; }
        [BindNever] public string? ChildPriceRaw { get; set; }
        [BindNever] public string? CapacityRaw { get; set; }
        [BindNever] public TourDeparture Departure { get; set; } = new();

        // ── Bước hiện tại: 1 = Tour, 2 = Departure ───────────────────────────
        [BindNever] public int Step { get; set; } = 1;

        // ── Danh sách dịch vụ dùng ServiceItem (Name/Address từ bảng Service) ─
        public List<string> EndPlaces { get; set; } = new();
        public List<ServiceItem> Hotels { get; set; } = new();
        public List<ServiceItem> Restaurants { get; set; } = new();
        public List<ServiceItem> Entertainments { get; set; } = new();

        // ── Dịch vụ đã chọn — lưu ServiceId ─────────────────────────────────
        [BindNever] public List<int> SelectedHotels { get; set; } = new();
        [BindNever] public List<int> SelectedRestaurants { get; set; } = new();
        [BindNever] public List<int> SelectedEntertainments { get; set; } = new();

        // ═════════════════════════════════════════════════════════════════════
        //  GET → Bước 1
        // ═════════════════════════════════════════════════════════════════════
        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            Step = 1;
            LoadServices(userId.Value);
            return Page();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST – Bước 1 → validate Tour, chuyển sang bước 2
        // ═════════════════════════════════════════════════════════════════════
        public async Task<IActionResult> OnPostNextAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            LoadServices(userId.Value);
            ModelState.Clear();
            Step = 1;

            ReadTourFromForm();
            var existingImageFromForm = Request.Form["ExistingImage"].ToString();
            await HandleImageUploadAsync(existingImageFromForm);

            ValidateTour(ExistingImage);
            if (!ModelState.IsValid)
                return Page();

            Step = 2;
            return Page();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST – Bước 2 → validate Departure, lưu Tour + Departure cùng lúc
        // ═════════════════════════════════════════════════════════════════════
        public async Task<IActionResult> OnPostSaveAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            LoadServices(userId.Value);
            ModelState.Clear();
            Step = 2;

            ReadTourFromForm();
            ExistingImage = Request.Form["ExistingImage"].ToString();
            Tour.Image = ExistingImage;

            ReadDepartureFromForm();

            ValidateDeparture();
            if (!ModelState.IsValid)
                return Page();

            // Tìm TravelAgent
            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (agent == null)
            {
                ModelState.AddModelError("", "Không tìm thấy Travel Agent.");
                return Page();
            }

            // Lưu Tour
            Tour.TravelAgentId = agent.TravelAgentId;
            Tour.Rate = 0;
            Tour.Status = 1;
            _context.Tours.Add(Tour);
            await _context.SaveChangesAsync(); // Tour.TourId có giá trị từ đây

            // Lưu TourServiceDetail
            await SaveServicesAsync();

            // Lưu Departure
            Departure.TourId = Tour.TourId;
            Departure.AvailableSeat = Departure.Capacity;
            Departure.Status = "active";
            _context.TourDepartures.Add(Departure);
            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["CreateSuccess"] = true;
            return RedirectToPage("./Index");
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private void ReadTourFromForm()
        {
            Tour = new Tour
            {
                TourName = Request.Form["Tour.TourName"].ToString().Trim(),
                StartPlace = Request.Form["Tour.StartPlace"].ToString().Trim(),
                EndPlace = Request.Form["Tour.EndPlace"].ToString().Trim(),
                TourIntroduce = Request.Form["Tour.TourIntroduce"].ToString().Trim(),
                TourSchedule = Request.Form["Tour.TourSchedule"].ToString().Trim(),
                TourInclude = Request.Form["Tour.TourInclude"].ToString().Trim(),
                TourNonInclude = Request.Form["Tour.TourNonInclude"].ToString().Trim(),
                NumberOfDay = int.TryParse(Request.Form["Tour.NumberOfDay"], out var nd) ? nd : 0,
            };

            // value trong <option> là ServiceId
            SelectedHotels = Request.Form["SelectedHotels"]
                .Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();
            SelectedRestaurants = Request.Form["SelectedRestaurants"]
                .Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();
            SelectedEntertainments = Request.Form["SelectedEntertainments"]
                .Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();
        }

        private void ReadDepartureFromForm()
        {
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
        }

        private async Task HandleImageUploadAsync(string existingImageFromForm)
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "images", "tour");
                Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
                Tour.Image = "images/tour/" + fileName;
                ExistingImage = Tour.Image;
            }
            else if (!string.IsNullOrEmpty(existingImageFromForm))
            {
                Tour.Image = existingImageFromForm;
                ExistingImage = existingImageFromForm;
            }
        }

        /// <summary>
        /// SelectedXxx đã chứa ServiceId trực tiếp → ghi thẳng vào TourServiceDetail
        /// không cần lookup sub-table nữa.
        /// </summary>
        private async Task SaveServicesAsync()
        {
            foreach (var serviceId in SelectedHotels.Where(x => x > 0))
                _context.TourServiceDetails.Add(new TourServiceDetail
                {
                    TourId = Tour.TourId,
                    ServiceId = serviceId,
                    ServiceName = "Accommodation"
                });

            foreach (var serviceId in SelectedRestaurants.Where(x => x > 0))
                _context.TourServiceDetails.Add(new TourServiceDetail
                {
                    TourId = Tour.TourId,
                    ServiceId = serviceId,
                    ServiceName = "Restaurant"
                });

            foreach (var serviceId in SelectedEntertainments.Where(x => x > 0))
                _context.TourServiceDetails.Add(new TourServiceDetail
                {
                    TourId = Tour.TourId,
                    ServiceId = serviceId,
                    ServiceName = "Entertainment"
                });

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// JOIN Service ←→ sub-table để phân loại.
        /// Name và Address đều từ bảng Service (serviceType: 1=Accommodation, 2=Restaurant, 3=Entertainment).
        /// </summary>
        private void LoadServices(int userId)
        {
            var agent = _context.TravelAgents.FirstOrDefault(a => a.UserId == userId);
            if (agent == null) return;

            // Lấy ServiceId thuộc từng sub-type
            var hotelSids = _context.Accommodations
                .Select(a => a.ServiceId).ToHashSet();
            var restaurantSids = _context.Restaurants
                .Select(r => r.ServiceId).ToHashSet();
            var entertainmentSids = _context.Entertainments
                .Select(e => e.ServiceId).ToHashSet();

            // Lấy tất cả Service của agent đang hoạt động + project thành ServiceItem
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
        }

        // ── Validate Tour ─────────────────────────────────────────────────────
        private void ValidateTour(string? existingImage = null)
        {
            if (string.IsNullOrWhiteSpace(Tour.TourName))
                ModelState.AddModelError("Tour.TourName", "Tên tour không được để trống!");
            else if (Tour.TourName.Trim().Length < 2 || Tour.TourName.Trim().Length > 255)
                ModelState.AddModelError("Tour.TourName", "Tên tour phải từ 2 đến 255 ký tự!");

            if (string.IsNullOrWhiteSpace(Tour.StartPlace))
                ModelState.AddModelError("Tour.StartPlace", "Điểm bắt đầu không được để trống!");

            if (string.IsNullOrWhiteSpace(Tour.EndPlace))
                ModelState.AddModelError("Tour.EndPlace", "Điểm đến không được để trống!");

            if (Tour.NumberOfDay <= 0)
                ModelState.AddModelError("Tour.NumberOfDay", "Số ngày phải lớn hơn 0!");
            else if (Tour.NumberOfDay > 365)
                ModelState.AddModelError("Tour.NumberOfDay", "Số ngày không được vượt quá 365!");

            if (string.IsNullOrWhiteSpace(Tour.TourIntroduce))
                ModelState.AddModelError("Tour.TourIntroduce", "Giới thiệu tour không được để trống!");
            else if (Tour.TourIntroduce.Trim().Length < 10 || Tour.TourIntroduce.Trim().Length > 3000)
                ModelState.AddModelError("Tour.TourIntroduce", "Giới thiệu tour phải từ 10 đến 3000 ký tự!");

            if (string.IsNullOrWhiteSpace(Tour.TourSchedule))
                ModelState.AddModelError("Tour.TourSchedule", "Lịch trình tour không được để trống!");
            else if (Tour.TourSchedule.Trim().Length < 10 || Tour.TourSchedule.Trim().Length > 5000)
                ModelState.AddModelError("Tour.TourSchedule", "Lịch trình tour phải từ 10 đến 5000 ký tự!");

            if (string.IsNullOrWhiteSpace(Tour.TourInclude))
                ModelState.AddModelError("Tour.TourInclude", "Mục bao gồm không được để trống!");
            else if (Tour.TourInclude.Trim().Length < 10 || Tour.TourInclude.Trim().Length > 2000)
                ModelState.AddModelError("Tour.TourInclude", "Mục bao gồm phải từ 10 đến 2000 ký tự!");

            if (string.IsNullOrWhiteSpace(Tour.TourNonInclude))
                ModelState.AddModelError("Tour.TourNonInclude", "Mục không bao gồm không được để trống!");
            else if (Tour.TourNonInclude.Trim().Length < 10 || Tour.TourNonInclude.Trim().Length > 2000)
                ModelState.AddModelError("Tour.TourNonInclude", "Mục không bao gồm phải từ 10 đến 2000 ký tự!");

            if ((ImageFile == null || ImageFile.Length == 0) && string.IsNullOrEmpty(existingImage))
                ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh cho tour!");
        }

        // ── Validate Departure ────────────────────────────────────────────────
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
