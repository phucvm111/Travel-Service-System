using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
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
        public Restaurant Restaurant { get; set; } = new Restaurant();

        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (agent == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đại lý.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Service.ServiceName))
                ModelState.AddModelError("Service.ServiceName", "Tên nhà hàng không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.Address))
                ModelState.AddModelError("Service.Address", "Địa chỉ không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.PhoneNumber))
                ModelState.AddModelError("Service.PhoneNumber", "Số điện thoại không được để trống.");

            if (string.IsNullOrWhiteSpace(Restaurant.RestaurantType))
                ModelState.AddModelError("Restaurant.RestaurantType", "Loại nhà hàng không được để trống.");

            if (Restaurant.OpenTime == null)
                ModelState.AddModelError("Restaurant.OpenTime", "Giờ mở cửa không được để trống.");

            if (Restaurant.CloseTime == null)
                ModelState.AddModelError("Restaurant.CloseTime", "Giờ đóng cửa không được để trống.");

            if (Restaurant.OpenTime != null && Restaurant.CloseTime != null
                && Restaurant.OpenTime >= Restaurant.CloseTime)
            {
                ModelState.AddModelError(string.Empty, "Giờ mở cửa phải nhỏ hơn giờ đóng cửa.");
            }

            ModelState.Remove("Restaurant.Service");
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
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "restaurants");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(UploadImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadImage.CopyToAsync(stream);
                }

                imagePath = "/images/restaurants/" + fileName;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Service.AgentId = agent.TravelAgentId;
                Service.ServiceType = 3; // nhớ kiểm tra 3 có đúng là Restaurant không
                Service.Image = imagePath;
                Service.Status = 1;
                Service.CreatedAt = DateTime.Now;
                Service.UpdatedAt = DateTime.Now;

                _context.Services.Add(Service);
                await _context.SaveChangesAsync();

                Restaurant.ServiceId = Service.ServiceId;

                _context.Restaurants.Add(Restaurant);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"ĐÃ THÊM NHÀ HÀNG THÀNH CÔNG ";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi thêm nhà hàng: " + ex.Message;
                return Page();
            }
        }
    }
}