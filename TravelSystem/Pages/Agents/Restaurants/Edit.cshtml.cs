using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
{
    public class EditModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(FinalPrnContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public int RestaurantId { get; set; }

        [BindProperty]
        public int ServiceId { get; set; }

        [BindProperty]
        public string? ExistingImage { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Tên nhà hàng không được để trống")]
        [StringLength(255)]
        [Display(Name = "Tên nhà hàng")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Mô tả không được để trống")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Giờ mở cửa")]
        public string? TimeOpen { get; set; }

        [BindProperty]
        [Display(Name = "Giờ đóng cửa")]
        public string? TimeClose { get; set; }

        [BindProperty]
        [Display(Name = "Hình ảnh mới")]
        public IFormFile? ImageFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? agentId = HttpContext.Session.GetInt32("UserID");
            if (agentId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Service)
                .FirstOrDefaultAsync(r =>
                    r.RestaurantId == id &&
                    r.Service != null &&
                    r.Service.TravelAgentId == agentId);

            if (restaurant == null)
            {
                return NotFound();
            }

            RestaurantId = restaurant.RestaurantId;
            ServiceId = restaurant.ServiceId;
            Name = restaurant.Name ?? string.Empty;
            Address = restaurant.Address ?? string.Empty;
            Phone = restaurant.Phone ?? string.Empty;
            Description = restaurant.Description ?? string.Empty;
            ExistingImage = restaurant.Image;
            TimeOpen = restaurant.TimeOpen?.ToString("HH:mm");
            TimeClose = restaurant.TimeClose?.ToString("HH:mm");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? agentId = HttpContext.Session.GetInt32("UserID");
            if (agentId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            Name = Name?.Trim() ?? string.Empty;
            Address = Address?.Trim() ?? string.Empty;
            Phone = Phone?.Trim() ?? string.Empty;
            Description = Description?.Trim() ?? string.Empty;

            if (!ValidateTime(TimeOpen, TimeClose))
            {
                ModelState.AddModelError("TimeClose", "Giờ đóng cửa phải sau giờ mở cửa.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Service)
                .FirstOrDefaultAsync(r =>
                    r.RestaurantId == RestaurantId &&
                    r.Service != null &&
                    r.Service.TravelAgentId == agentId);

            if (restaurant == null)
            {
                return NotFound();
            }

            restaurant.Name = Name;
            restaurant.Address = Address;
            restaurant.Phone = Phone;
            restaurant.Description = Description;
            restaurant.TimeOpen = ParseTime(TimeOpen);
            restaurant.TimeClose = ParseTime(TimeClose);

            if (ImageFile != null && ImageFile.Length > 0)
            {
                restaurant.Image = await SaveImageAsync(ImageFile);
            }

            if (restaurant.Service != null)
            {
                restaurant.Service.ServiceName = Name;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật nhà hàng thành công.";
            return RedirectToPage("./Index");
        }

        private async Task<string?> SaveImageAsync(IFormFile file)
        {
            string folder = Path.Combine(_env.WebRootPath, "images", "restaurants");
            Directory.CreateDirectory(folder);

            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/restaurants/{fileName}";
        }

        private static bool ValidateTime(string? open, string? close)
        {
            if (string.IsNullOrWhiteSpace(open) || string.IsNullOrWhiteSpace(close))
            {
                return true;
            }

            if (!TimeOnly.TryParse(open, out var openTime) || !TimeOnly.TryParse(close, out var closeTime))
            {
                return true;
            }

            return openTime < closeTime;
        }

        private static TimeOnly? ParseTime(string? value)
        {
            return TimeOnly.TryParse(value, out var time) ? time : null;
        }
    }
}