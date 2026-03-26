using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Entertainments
{
    public class CreateModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(FinalPrnContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Service Service { get; set; } = new Service();

        [BindProperty]
        public Entertainment Entertainment { get; set; } = new Entertainment();

        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        public List<SelectListItem> EntertainmentNameOptions { get; set; } = new();

        private void LoadEntertainmentNameOptions()
        {
            EntertainmentNameOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Chọn tên dịch vụ --" },
                new SelectListItem { Value = "Tham quan", Text = "Tham quan" },
                new SelectListItem { Value = "Vui chơi", Text = "Vui chơi" },
                new SelectListItem { Value = "Công viên", Text = "Công viên" },
                new SelectListItem { Value = "Rạp chiếu phim", Text = "Rạp chiếu phim" },
                new SelectListItem { Value = "Khu giải trí", Text = "Khu giải trí" },
                new SelectListItem { Value = "Khác", Text = "Khác" }
            };
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            LoadEntertainmentNameOptions();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            LoadEntertainmentNameOptions();

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (agent == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đại lý.";
                return RedirectToPage("/Error");
            }

            if (string.IsNullOrWhiteSpace(Service.ServiceName))
                ModelState.AddModelError("Service.ServiceName", "Vui lòng chọn tên dịch vụ.");

            if (string.IsNullOrWhiteSpace(Service.Address))
                ModelState.AddModelError("Service.Address", "Địa chỉ không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.PhoneNumber))
                ModelState.AddModelError("Service.PhoneNumber", "Số điện thoại không được để trống.");

            if (Entertainment.OpenTime == null)
                ModelState.AddModelError("Entertainment.OpenTime", "Giờ mở cửa không được để trống.");

            if (Entertainment.CloseTime == null)
                ModelState.AddModelError("Entertainment.CloseTime", "Giờ đóng cửa không được để trống.");

            if (Entertainment.OpenTime != null && Entertainment.CloseTime != null
                && Entertainment.OpenTime >= Entertainment.CloseTime)
            {
                ModelState.AddModelError(string.Empty, "Giờ mở cửa phải nhỏ hơn giờ đóng cửa.");
            }

            if (Entertainment.TicketPrice.HasValue && Entertainment.TicketPrice < 0)
            {
                ModelState.AddModelError("Entertainment.TicketPrice", "Giá vé phải lớn hơn hoặc bằng 0.");
            }

            ModelState.Remove("Entertainment.Service");
            ModelState.Remove("Service.Agent");
            ModelState.Remove("Service.Accommodation");
            ModelState.Remove("Service.Entertainment");
            ModelState.Remove("Service.Restaurant");
            ModelState.Remove("Service.TourServiceDetails");

            if (!ModelState.IsValid)
                return Page();

            string? imagePath = null;

            if (UploadImage != null && UploadImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "entertainments");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(UploadImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadImage.CopyToAsync(stream);
                }

                imagePath = "/images/entertainments/" + fileName;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Service.AgentId = agent.TravelAgentId;
                Service.ServiceType = 2;
                Service.Image = imagePath;
                Service.Status = 1;
                Service.CreatedAt = DateTime.Now;
                Service.UpdatedAt = DateTime.Now;

                _context.Services.Add(Service);
                await _context.SaveChangesAsync();

                Entertainment.ServiceId = Service.ServiceId;

                _context.Entertainments.Add(Entertainment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "ĐÃ THÊM DỊCH VỤ THÀNH CÔNG";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi thêm dịch vụ giải trí: " + ex.Message;
                return Page();
            }
        }
    }
}