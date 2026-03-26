using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
{
    public class EditModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditModel(FinalPrnContext context, IWebHostEnvironment environment)
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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (agent == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đại lý.";
                return RedirectToPage("/Error");
            }

            Restaurant = await _context.Restaurants
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                                       && x.Service != null
                                       && x.Service.AgentId == agent.TravelAgentId
                                       && x.Service.ServiceType == 3);

            if (Restaurant == null || Restaurant.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà hàng.";
                return RedirectToPage("./Index");
            }

            Service = Restaurant.Service;

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
                return RedirectToPage("/Error");
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

            var restaurantInDb = await _context.Restaurants
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == Restaurant.ServiceId
                                       && x.Service != null
                                       && x.Service.AgentId == agent.TravelAgentId
                                       && x.Service.ServiceType == 3);

            if (restaurantInDb == null || restaurantInDb.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà hàng để cập nhật.";
                return RedirectToPage("./Index");
            }

            string? imagePath = restaurantInDb.Service.Image;

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
                restaurantInDb.Service.ServiceName = Service.ServiceName;
                restaurantInDb.Service.Address = Service.Address;
                restaurantInDb.Service.PhoneNumber = Service.PhoneNumber;
                restaurantInDb.Service.Description = Service.Description;
                restaurantInDb.Service.Image = imagePath;
                restaurantInDb.Service.Status = Service.Status;
                restaurantInDb.Service.UpdatedAt = DateTime.Now;

                restaurantInDb.RestaurantType = Restaurant.RestaurantType;
                restaurantInDb.OpenTime = Restaurant.OpenTime;
                restaurantInDb.CloseTime = Restaurant.CloseTime;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Cập nhật nhà hàng thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi cập nhật nhà hàng: " + ex.Message;
                return Page();
            }
        }
    }
}