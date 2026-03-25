using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
{
    public class CreateModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(FinalPrnContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

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
        [Display(Name = "Hình ảnh")]
        public IFormFile? ImageFile { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
            {
                return RedirectToPage("/Auths/Login");
            }

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

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var service = new Service
                {
                    ServiceName = Name,
                    ServiceType = "RESTAURANT",
                    TravelAgentId = agentId.Value
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                var restaurant = new Restaurant
                {
                    ServiceId = service.ServiceId,
                    Name = Name,
                    Address = Address,
                    Phone = Phone,
                    Description = Description,
                    Image = await SaveImageAsync(ImageFile),
                    TimeOpen = ParseTime(TimeOpen),
                    TimeClose = ParseTime(TimeClose)
                };

                _context.Restaurants.Add(restaurant);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] = "Đã thêm nhà hàng thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return Page();
            }
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

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