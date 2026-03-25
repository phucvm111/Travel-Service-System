using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    public class CreateModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<HubServer> _hub;

        public CreateModel(Prn222PrjContext context, IWebHostEnvironment env, IHubContext<HubServer> hub)
        {
            _context = context;
            _env = env;
            _hub = hub;
        }

        // ── Bind từ form ──────────────────────────────────────────────────────
        [BindNever] public Tour Tour { get; set; } = new();
        [BindProperty] public IFormFile? ImageFile { get; set; }

        // Raw string từ form (giá, số lượng đã format kiểu VN)
        [BindNever] public string? AdultPriceRaw { get; set; }
        [BindNever] public string? ChildrenPriceRaw { get; set; }
        [BindNever] public string? QuantityRaw { get; set; }

        [BindNever] public string? ExistingImage { get; set; }

        // ── Danh sách dịch vụ / điểm đến để render select ────────────────────
        public List<string> EndPlaces { get; set; } = new();
        public List<Accommodation> Hotels { get; set; } = new();
        public List<Restaurant> Restaurants { get; set; } = new();
        public List<Entertainment> Entertainments { get; set; } = new();

        // ── Dịch vụ đã chọn (giữ lại khi validation fail) ────────────────────
        [BindNever] public List<int> SelectedHotels { get; set; } = new();
        [BindNever] public List<int> SelectedRestaurants { get; set; } = new();
        [BindNever] public List<int> SelectedEntertainments { get; set; } = new();



        // ═════════════════════════════════════════════════════════════════════
        //  GET
        // ═════════════════════════════════════════════════════════════════════
        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            LoadServices(userId.Value);
            return Page();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST
        // ═════════════════════════════════════════════════════════════════════
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            // 1. Load lại danh sách dịch vụ để render lại form nếu có lỗi
            LoadServices(userId.Value);
            ModelState.Clear();

            // 2. Đọc tất cả dữ liệu từ Request.Form
            Tour = new Tour
            {
                TourName = Request.Form["Tour.TourName"].ToString().Trim(),
                StartPlace = Request.Form["Tour.StartPlace"].ToString().Trim(),
                EndPlace = Request.Form["Tour.EndPlace"].ToString().Trim(),
                TourIntroduce = Request.Form["Tour.TourIntroduce"].ToString().Trim(),
                TourSchedule = Request.Form["Tour.TourSchedule"].ToString().Trim(),
                TourInclude = Request.Form["Tour.TourInclude"].ToString().Trim(),
                TourNonInclude = Request.Form["Tour.TourNonInclude"].ToString().Trim(),
                StartDay = DateOnly.TryParse(Request.Form["Tour.StartDay"], out var sd) ? sd : null,
                EndDay = DateOnly.TryParse(Request.Form["Tour.EndDay"], out var ed) ? ed : null,
            };

            AdultPriceRaw = Request.Form["AdultPriceRaw"].ToString().Trim();
            ChildrenPriceRaw = Request.Form["ChildrenPriceRaw"].ToString().Trim();
            QuantityRaw = Request.Form["QuantityRaw"].ToString().Trim();

            // Parse giá — bỏ dấu chấm/phẩy format VN 
            Tour.AdultPrice = double.TryParse(AdultPriceRaw.Replace(".", "").Replace(",", ""), out double ap) ? ap : 0;
            Tour.ChildrenPrice = double.TryParse(ChildrenPriceRaw.Replace(".", "").Replace(",", ""), out double cp) ? cp : 0;
            Tour.Quantity = int.TryParse(QuantityRaw, out int qty) ? qty : 0;

            // Dịch vụ đã chọn
            SelectedHotels = Request.Form["SelectedHotels"].Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();
            SelectedRestaurants = Request.Form["SelectedRestaurants"].Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();
            SelectedEntertainments = Request.Form["SelectedEntertainments"].Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();

            // 3. Xử lý ảnh
            var existingImageFromForm = Request.Form["ExistingImage"].ToString();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Upload ảnh mới → lưu vào wwwroot/images/tour/
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

            // 4. Validate — truyền existingImage để không bắt upload lại ảnh
            ValidateInput(ExistingImage);
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 5. Tìm TravelAgent
            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (agent == null)
            {
                ModelState.AddModelError("", "Không tìm thấy Travel Agent");
                return Page();
            }

            // 6. Tính số ngày, gán thêm field
            Tour.NumberOfDay =
                (Tour.EndDay!.Value.ToDateTime(TimeOnly.MinValue)
               - Tour.StartDay!.Value.ToDateTime(TimeOnly.MinValue)).Days + 1;

            Tour.TravelAgentId = agent.TravelAgentId;
            Tour.Rate = 0;
            Tour.Status = 1;

            // 7. Lưu DB
            _context.Tours.Add(Tour);
            await _context.SaveChangesAsync();
            await SaveServices();

            // 8. Thông báo SignalR
            await _hub.Clients.All.SendAsync("loadAll");

            // 9.Dùng TempData truyền sang Index, Index sẽ hiện SweetAlert
            TempData["CreateSuccess"] = true;
            return RedirectToPage("./Index");
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Lưu dịch vụ đã chọn vào TourServiceDetail 
        private async Task SaveServices()
        {
            foreach (var id in SelectedHotels.Where(x => x > 0))
            {
                var sid = _context.Accommodations
                    .Where(a => a.AccommodationId == id)
                    .Select(a => a.ServiceId).FirstOrDefault();
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = Tour.TourId, ServiceId = sid, ServiceName = "Accommodation" });
            }
            foreach (var id in SelectedRestaurants.Where(x => x > 0))
            {
                var sid = _context.Restaurants
                    .Where(r => r.RestaurantId == id)
                    .Select(r => r.ServiceId).FirstOrDefault();
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = Tour.TourId, ServiceId = sid, ServiceName = "Restaurant" });
            }
            foreach (var id in SelectedEntertainments.Where(x => x > 0))
            {
                var sid = _context.Entertainments
                    .Where(e => e.EntertainmentId == id)
                    .Select(e => e.ServiceId).FirstOrDefault();
                _context.TourServiceDetails.Add(new TourServiceDetail
                { TourId = Tour.TourId, ServiceId = sid, ServiceName = "Entertainment" });
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Load danh sách dịch vụ theo agent
        /// </summary>
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
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// existingImage: đường dẫn ảnh cũ, nếu có thì không báo lỗi ảnh.
        /// </summary>
        private void ValidateInput(string? existingImage = null)
        {
            // ── Tên tour ──────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(Tour.TourName))
                ModelState.AddModelError("Tour.TourName", "Tên tour không được để trống!");
            else if (Tour.TourName.Trim().Length < 2 || Tour.TourName.Trim().Length > 255)
                ModelState.AddModelError("Tour.TourName", "Tên tour phải từ 2 đến 255 ký tự!");

            // ── Điểm bắt đầu / điểm đến ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(Tour.StartPlace))
                ModelState.AddModelError("Tour.StartPlace", "Điểm bắt đầu không được để trống!");

            if (string.IsNullOrWhiteSpace(Tour.EndPlace))
                ModelState.AddModelError("Tour.EndPlace", "Điểm đến không được để trống!");

            // ── Ngày bắt đầu ──────────────────────────────────────────────────
            if (Tour.StartDay == null)
                ModelState.AddModelError("Tour.StartDay", "Ngày bắt đầu không được để trống!");
            else if (Tour.StartDay.Value <= DateOnly.FromDateTime(DateTime.Today))
                ModelState.AddModelError("Tour.StartDay", "Ngày bắt đầu phải sau ngày hiện tại!");

            // ── Ngày kết thúc ─────────────────────────────────────────────────
            if (Tour.EndDay == null)
                ModelState.AddModelError("Tour.EndDay", "Ngày kết thúc không được để trống!");
            else if (Tour.StartDay != null && Tour.EndDay < Tour.StartDay)
                ModelState.AddModelError("Tour.EndDay", "Ngày kết thúc phải sau ngày bắt đầu!");

            // ── Giá người lớn ─────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(AdultPriceRaw) || Tour.AdultPrice <= 0)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn phải lớn hơn 0!");
            else if (Tour.AdultPrice > 100_000_000)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn không được vượt quá 100 triệu đồng!");

            // ── Giá trẻ em ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(ChildrenPriceRaw) || Tour.ChildrenPrice < 0)
                ModelState.AddModelError("ChildrenPriceRaw", "Giá trẻ em không hợp lệ!");
            else if (Tour.ChildrenPrice > 100_000_000)
                ModelState.AddModelError("ChildrenPriceRaw", "Giá trẻ em không được vượt quá 100 triệu đồng!");

            // ── So sánh giá người lớn > giá trẻ em ───────────────────────────
            if (!string.IsNullOrWhiteSpace(AdultPriceRaw) && !string.IsNullOrWhiteSpace(ChildrenPriceRaw)
                && Tour.AdultPrice > 0 && Tour.ChildrenPrice >= 0
                && Tour.AdultPrice <= Tour.ChildrenPrice)
                ModelState.AddModelError("AdultPriceRaw", "Giá người lớn phải lớn hơn giá trẻ em!");

            // ── Số lượng ──────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(QuantityRaw) || Tour.Quantity <= 0)
                ModelState.AddModelError("QuantityRaw", "Số lượng phải lớn hơn 0!");
            else if (Tour.Quantity > 1000)
                ModelState.AddModelError("QuantityRaw", "Số lượng không được vượt quá 1000!");

            // ── Giới thiệu ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(Tour.TourIntroduce))
                ModelState.AddModelError("Tour.TourIntroduce", "Giới thiệu tour không được để trống!");
            else if (Tour.TourIntroduce.Trim().Length < 10 || Tour.TourIntroduce.Trim().Length > 3000)
                ModelState.AddModelError("Tour.TourIntroduce", "Giới thiệu tour phải từ 10 đến 3000 ký tự!");

            // ── Lịch trình ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(Tour.TourSchedule))
                ModelState.AddModelError("Tour.TourSchedule", "Lịch trình tour không được để trống!");
            else if (Tour.TourSchedule.Trim().Length < 10 || Tour.TourSchedule.Trim().Length > 5000)
                ModelState.AddModelError("Tour.TourSchedule", "Lịch trình tour phải từ 10 đến 5000 ký tự!");

            // ── Bao gồm ───────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(Tour.TourInclude))
                ModelState.AddModelError("Tour.TourInclude", "Mục bao gồm không được để trống!");
            else if (Tour.TourInclude.Trim().Length < 10 || Tour.TourInclude.Trim().Length > 2000)
                ModelState.AddModelError("Tour.TourInclude", "Mục bao gồm phải từ 10 đến 2000 ký tự!");

            // ── Không bao gồm ─────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(Tour.TourNonInclude))
                ModelState.AddModelError("Tour.TourNonInclude", "Mục không bao gồm không được để trống!");
            else if (Tour.TourNonInclude.Trim().Length < 10 || Tour.TourNonInclude.Trim().Length > 2000)
                ModelState.AddModelError("Tour.TourNonInclude", "Mục không bao gồm phải từ 10 đến 2000 ký tự!");

            // ── Ảnh ───────────────────────────────────────────────────────────
            // Chỉ báo lỗi khi KHÔNG có ảnh mới VÀ KHÔNG có ảnh cũ được giữ lại
            if ((ImageFile == null || ImageFile.Length == 0) && string.IsNullOrEmpty(existingImage))
                ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh cho tour!");


        }
    }
}
