using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Accommodations
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
        public Accommodation Accommodation { get; set; } = new();

        [BindProperty]
        public Service Service { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        [BindProperty]
        public string? ExistingImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(x => x.UserId == userId.Value);
            if (agent == null)
                return RedirectToPage("/Error");

            var item = await _context.Accommodations
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                    && x.Service != null
                    && x.Service.AgentId == agent.TravelAgentId
                    && x.Service.ServiceType == 1);

            if (item == null)
                return NotFound();

            Accommodation = new Accommodation
            {
                ServiceId = item.ServiceId,
                CheckInTime = item.CheckInTime,
                CheckOutTime = item.CheckOutTime,
                StarRating = item.StarRating
            };

            Service = new Service
            {
                ServiceId = item.Service!.ServiceId,
                ServiceName = item.Service.ServiceName,
                Address = item.Service.Address,
                PhoneNumber = item.Service.PhoneNumber,
                Description = item.Service.Description,
                Status = item.Service.Status
            };

            ExistingImage = item.Service.Image;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(x => x.UserId == userId.Value);
            if (agent == null)
                return RedirectToPage("/Error");

            ValidateInput();

            if (!ModelState.IsValid)
                return Page();

            var item = await _context.Accommodations
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == Accommodation.ServiceId
                    && x.Service != null
                    && x.Service.AgentId == agent.TravelAgentId
                    && x.Service.ServiceType == 1);

            if (item == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy khách sạn.");
                return Page();
            }

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var ext = Path.GetExtension(ImageFile.FileName).ToLower();
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                    if (!allowedExt.Contains(ext))
                    {
                        ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh .jpg, .jpeg, .png, .webp.");
                        return Page();
                    }

                    var folder = Path.Combine(_env.WebRootPath, "images", "accommodation");
                    Directory.CreateDirectory(folder);

                    var fileName = Guid.NewGuid() + ext;
                    var filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    item.Service!.Image = $"images/accommodation/{fileName}";
                }
                else
                {
                    item.Service!.Image = ExistingImage;
                }

                item.CheckInTime = Accommodation.CheckInTime;
                item.CheckOutTime = Accommodation.CheckOutTime;
                item.StarRating = Accommodation.StarRating;

                item.Service!.ServiceName = Service.ServiceName;
                item.Service.Address = Service.Address;
                item.Service.PhoneNumber = Service.PhoneNumber;
                item.Service.Description = Service.Description;
                item.Service.Status = Service.Status;
                item.Service.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật khách sạn thành công.";
                return RedirectToPage("./Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật.");
                return Page();
            }
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Service.ServiceName))
                ModelState.AddModelError("Service.ServiceName", "Tên khách sạn không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.Address))
                ModelState.AddModelError("Service.Address", "Địa chỉ không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.PhoneNumber))
                ModelState.AddModelError("Service.PhoneNumber", "Số điện thoại không được để trống.");

            if (string.IsNullOrWhiteSpace(Service.Description))
                ModelState.AddModelError("Service.Description", "Mô tả không được để trống.");

            if (Accommodation.CheckInTime == null)
                ModelState.AddModelError("Accommodation.CheckInTime", "Giờ check-in không được để trống.");

            if (Accommodation.CheckOutTime == null)
                ModelState.AddModelError("Accommodation.CheckOutTime", "Giờ check-out không được để trống.");
        }
    }
}