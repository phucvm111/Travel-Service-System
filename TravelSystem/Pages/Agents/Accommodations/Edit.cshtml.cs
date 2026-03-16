using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Accommodations
{
    public class EditModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(Prn222PrjContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindNever]
        public Accommodation Accommodation { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        [BindNever]
        public string? ExistingImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (agent == null) return RedirectToPage("/Error");

            Accommodation = await _context.Accommodations
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.AccommodationId == id
                    && a.Service != null
                    && a.Service.TravelAgentId == agent.TravelAgentId
                    && a.Service.ServiceType == "ACCOMMODATION");

            if (Accommodation == null) return NotFound();

            ExistingImage = Accommodation.Image;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            ModelState.Clear();

            int accommodationId = int.TryParse(Request.Form["Accommodation.AccommodationId"], out var id) ? id : 0;

            Accommodation = new Accommodation
            {
                AccommodationId = accommodationId,
                Name = Request.Form["Accommodation.Name"].ToString().Trim(),
                Address = Request.Form["Accommodation.Address"].ToString().Trim(),
                Phone = Request.Form["Accommodation.Phone"].ToString().Trim(),
                Description = Request.Form["Accommodation.Description"].ToString().Trim(),
                CheckInTime = TimeOnly.TryParse(Request.Form["Accommodation.CheckInTime"], out var ci) ? ci : null,
                CheckOutTime = TimeOnly.TryParse(Request.Form["Accommodation.CheckOutTime"], out var co) ? co : null
            };

            var existingImageFromForm = Request.Form["ExistingImage"].ToString();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "images", "accommodation");
                Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(folder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await ImageFile.CopyToAsync(stream);

                Accommodation.Image = "images/accommodation/" + fileName;
                ExistingImage = Accommodation.Image;
            }
            else if (!string.IsNullOrEmpty(existingImageFromForm))
            {
                Accommodation.Image = existingImageFromForm;
                ExistingImage = existingImageFromForm;
            }

            ValidateInput(ExistingImage);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (agent == null)
            {
                ModelState.AddModelError("", "Không tìm thấy Travel Agent.");
                return Page();
            }

            var accommodationInDb = await _context.Accommodations
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.AccommodationId == Accommodation.AccommodationId
                    && a.Service != null
                    && a.Service.TravelAgentId == agent.TravelAgentId
                    && a.Service.ServiceType == "ACCOMMODATION");

            if (accommodationInDb == null)
            {
                ModelState.AddModelError("", "Không tìm thấy khách sạn để cập nhật.");
                return Page();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                accommodationInDb.Name = Accommodation.Name;
                accommodationInDb.Address = Accommodation.Address;
                accommodationInDb.Phone = Accommodation.Phone;
                accommodationInDb.Description = Accommodation.Description;
                accommodationInDb.CheckInTime = Accommodation.CheckInTime;
                accommodationInDb.CheckOutTime = Accommodation.CheckOutTime;
                accommodationInDb.Image = Accommodation.Image;

                if (accommodationInDb.Service != null)
                {
                    accommodationInDb.Service.ServiceName = Accommodation.Name;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["EditSuccess"] = true;
                return RedirectToPage("./Index");
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật khách sạn.");
                return Page();
            }
        }

        private void ValidateInput(string? existingImage = null)
        {
            if (Accommodation.AccommodationId <= 0)
                ModelState.AddModelError("", "Id khách sạn không hợp lệ.");

            if (string.IsNullOrWhiteSpace(Accommodation.Name))
                ModelState.AddModelError("Accommodation.Name", "Tên khách sạn không được để trống!");
            else if (Accommodation.Name.Trim().Length < 2 || Accommodation.Name.Trim().Length > 255)
                ModelState.AddModelError("Accommodation.Name", "Tên khách sạn phải từ 2 đến 255 ký tự!");

            if (string.IsNullOrWhiteSpace(Accommodation.Address))
                ModelState.AddModelError("Accommodation.Address", "Địa chỉ không được để trống!");
            else if (Accommodation.Address.Trim().Length < 5 || Accommodation.Address.Trim().Length > 255)
                ModelState.AddModelError("Accommodation.Address", "Địa chỉ phải từ 5 đến 255 ký tự!");

            if (string.IsNullOrWhiteSpace(Accommodation.Phone))
                ModelState.AddModelError("Accommodation.Phone", "Số điện thoại không được để trống!");
            else if (Accommodation.Phone.Trim().Length < 8 || Accommodation.Phone.Trim().Length > 20)
                ModelState.AddModelError("Accommodation.Phone", "Số điện thoại phải từ 8 đến 20 ký tự!");

            if (string.IsNullOrWhiteSpace(Accommodation.Description))
                ModelState.AddModelError("Accommodation.Description", "Mô tả không được để trống!");
            else if (Accommodation.Description.Trim().Length < 10 || Accommodation.Description.Trim().Length > 3000)
                ModelState.AddModelError("Accommodation.Description", "Mô tả phải từ 10 đến 3000 ký tự!");

            if (Accommodation.CheckInTime == null)
                ModelState.AddModelError("Accommodation.CheckInTime", "Giờ check-in không được để trống!");

            if (Accommodation.CheckOutTime == null)
                ModelState.AddModelError("Accommodation.CheckOutTime", "Giờ check-out không được để trống!");

            if (Accommodation.CheckInTime != null && Accommodation.CheckOutTime != null
                && Accommodation.CheckInTime >= Accommodation.CheckOutTime)
                ModelState.AddModelError("Accommodation.CheckOutTime", "Giờ check-out phải sau giờ check-in!");

            if ((ImageFile == null || ImageFile.Length == 0) && string.IsNullOrEmpty(existingImage))
                ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh cho khách sạn!");
        }
    }
}