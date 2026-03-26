using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Accommodations
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
        public Accommodation Accommodation { get; set; } = new();

        [BindProperty]
        public Service Service { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            Service.Status = 1;
            Service.ServiceType = 1;
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
                ModelState.AddModelError(string.Empty, "Không tìm thấy thông tin đại lý.");
                return Page();
            }

            Service.ServiceType = 1;

            ValidateInput();

            if (!ModelState.IsValid)
            {
                AddAllModelStateErrorsToSummary();
                return Page();
            }

            string? imagePath = null;

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var extension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(string.Empty, "Chỉ chấp nhận file ảnh .jpg, .jpeg, .png, .webp.");
                        return Page();
                    }

                    var folderPath = Path.Combine(_env.WebRootPath, "images", "accommodation");
                    Directory.CreateDirectory(folderPath);

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    imagePath = $"images/accommodation/{fileName}";
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                Service.AgentId = agent.TravelAgentId;
                Service.ServiceType = 1;
                Service.Image = imagePath;
                Service.Status ??= 1;
                Service.CreatedAt = DateTime.Now;
                Service.UpdatedAt = DateTime.Now;

                _context.Services.Add(Service);
                await _context.SaveChangesAsync();

                if (Service.ServiceId <= 0)
                {
                    throw new Exception("Không tạo được ServiceId cho khách sạn.");
                }

                Accommodation.ServiceId = Service.ServiceId;

                _context.Accommodations.Add(Accommodation);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Thêm khách sạn thành công.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                var dbMessage = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, "Lỗi database: " + dbMessage);
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống: " + ex.Message);
                return Page();
            }
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Service.ServiceName))
                ModelState.AddModelError(nameof(Service.ServiceName), "Tên khách sạn không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.Address))
                ModelState.AddModelError(nameof(Service.Address), "Địa chỉ không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.PhoneNumber))
                ModelState.AddModelError(nameof(Service.PhoneNumber), "Số điện thoại không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.Description))
                ModelState.AddModelError(nameof(Service.Description), "Mô tả không được để trống.");

            if (Accommodation.CheckInTime == null)
                ModelState.AddModelError(nameof(Accommodation.CheckInTime), "Giờ check-in không được để trống.");

            if (Accommodation.CheckOutTime == null)
                ModelState.AddModelError(nameof(Accommodation.CheckOutTime), "Giờ check-out không được để trống.");

            if (Accommodation.StarRating.HasValue &&
                (Accommodation.StarRating < 1 || Accommodation.StarRating > 5))
            {
                ModelState.AddModelError(nameof(Accommodation.StarRating), "Hạng sao phải từ 1 đến 5.");
            }
        }

        private void AddAllModelStateErrorsToSummary()
        {
            var errors = ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                .ToList();

            foreach (var err in errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
        }
    }
}