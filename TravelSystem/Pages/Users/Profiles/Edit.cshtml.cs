using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;
using System.Text.RegularExpressions;

namespace TravelSystem.Pages.Users.Profiles
{
    public class EditModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hub;
        private readonly IWebHostEnvironment _env;

        public EditModel(FinalPrnContext context, IHubContext<HubServer> hub, IWebHostEnvironment env)
        {
            _context = context;
            _hub = hub;
            _env = env;
        }

        [BindProperty]
        public User User { get; set; }

        // Tourist fields (Role 5)
        [BindProperty] public string? AccountHolderName { get; set; }
        [BindProperty] public string? IdCard { get; set; }
        [BindProperty] public string? BankName { get; set; }
        [BindProperty] public string? BankNumber { get; set; }
        [BindProperty] public IFormFile? FrontIdcardFile { get; set; }
        [BindProperty] public IFormFile? BackIdcardFile { get; set; }
        [BindProperty] public string? ExistingFrontIdcard { get; set; }
        [BindProperty] public string? ExistingBackIdcard { get; set; }

        // Staff fields (Role 4)
        [BindProperty] public int? EmployeeCode { get; set; }
        [BindProperty] public string? HireDateRaw { get; set; }
        [BindProperty] public string? WorkStatus { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            User = await _context.Users
                .Include(u => u.Tourist)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (User == null) return NotFound();

            // Load data based on Role
            if (User.RoleId == 5 && User.Tourist != null)
            {
                AccountHolderName = User.Tourist.AccountHolderName;
                IdCard = User.Tourist.IdCard;
                BankName = User.Tourist.BankName;
                BankNumber = User.Tourist.BankNumber;
                ExistingFrontIdcard = User.Tourist.IdCardFrontImage;
                ExistingBackIdcard = User.Tourist.IdCardBackImage;
            }
            else if (User.RoleId == 4 && User.Staff != null)
            {
                EmployeeCode = User.Staff.EmployeeCode;
                HireDateRaw = User.Staff.HireDate?.ToString("yyyy-MM-dd");
                WorkStatus = User.Staff.WorkStatus;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userInDb = await _context.Users
        .Include(u => u.Tourist)
        .Include(u => u.Staff)
        .FirstOrDefaultAsync(u => u.UserId == User.UserId);

            if (userInDb == null) return NotFound();
            User.RoleId = userInDb.RoleId;

            // Thực hiện Validate
            ModelState.Clear();
            ValidateInput();

            // 2. Validate theo Role
            if (userInDb.RoleId == 5) ValidateTourist();
            if (userInDb.RoleId == 4) ValidateEmployee();

            if (!ModelState.IsValid) return Page();

            // Update basic info
            userInDb.LastName = User.LastName?.Trim();
            userInDb.FirstName = User.FirstName?.Trim();
            userInDb.Dob = User.Dob;
            userInDb.Gender = User.Gender;
            userInDb.Address = User.Address?.Trim();
            userInDb.Phone = User.Phone?.Trim();
            userInDb.Password = User.Password?.Trim();
            userInDb.UpdateAt = DateOnly.FromDateTime(DateTime.Now);

            // Save Tourist Info (Role 5)
            if (userInDb.RoleId == 5)
            {
                if (userInDb.Tourist == null) userInDb.Tourist = new Tourist { TouristId = userInDb.UserId };
                userInDb.Tourist.AccountHolderName = AccountHolderName?.Trim();
                userInDb.Tourist.IdCard = IdCard?.Trim();
                userInDb.Tourist.BankName = BankName?.Trim();
                userInDb.Tourist.BankNumber = BankNumber?.Trim();

                if (FrontIdcardFile != null) userInDb.Tourist.IdCardFrontImage = await SaveFile(FrontIdcardFile, "images/tourist");
                if (BackIdcardFile != null) userInDb.Tourist.IdCardBackImage = await SaveFile(BackIdcardFile, "images/tourist");
            }
            // Save Staff Info (Role 4)
            else if (userInDb.RoleId == 4)
            {
                if (userInDb.Staff == null) userInDb.Staff = new Staff { StaffId = userInDb.UserId };
            }

            await _context.SaveChangesAsync();
            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToPage("./Details", new { id = userInDb.UserId });
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            string dir = Path.Combine(_env.WebRootPath, folder);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return folder + "/" + fileName;
        }

        private void ValidateInput()
        {
            // GIỮ NGUYÊN TOÀN BỘ LOGIC CỦA BẠN
            if (string.IsNullOrWhiteSpace(User.LastName))
                ModelState.AddModelError("User.LastName", "Họ không được để trống!");
            else if (!Regex.IsMatch(User.LastName, @"^[\p{L} ]{2,50}$"))
                ModelState.AddModelError("User.LastName", "Họ chỉ chứa chữ cái và khoảng trắng (2-50 ký tự)");

            if (string.IsNullOrWhiteSpace(User.FirstName))
                ModelState.AddModelError("User.FirstName", "Tên không được để trống!");
            else if (!Regex.IsMatch(User.FirstName, @"^[\p{L} ]{2,50}$"))
                ModelState.AddModelError("User.FirstName", "Tên chỉ chứa chữ cái và khoảng trắng (2-50 ký tự)");

            if (string.IsNullOrWhiteSpace(User.Address))
                ModelState.AddModelError("User.Address", "Địa chỉ không được để trống!");
            else if (User.Address.Length < 5 || User.Address.Length > 200)
                ModelState.AddModelError("User.Address", "Địa chỉ phải từ 5 đến 200 ký tự!");

            if (string.IsNullOrWhiteSpace(User.Phone))
                ModelState.AddModelError("User.Phone", "Số điện thoại không được để trống!");
            else if (!Regex.IsMatch(User.Phone, @"^0\d{9}$"))
                ModelState.AddModelError("User.Phone", "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 chữ số!");

            DateOnly today = DateOnly.FromDateTime(DateTime.Now);
            if (User.Dob == null)
                ModelState.AddModelError("User.Dob", "Ngày sinh không được để trống!");
            else
            {
                DateOnly dob = User.Dob.Value;
                if (dob > today) ModelState.AddModelError("User.Dob", "Ngày sinh không được lớn hơn hiện tại!");
                else if (dob.AddYears(18) > today) ModelState.AddModelError("User.Dob", "Bạn phải đủ 18 tuổi!");
                else if (dob < today.AddYears(-100)) ModelState.AddModelError("User.Dob", "Tuổi không được lớn hơn 100!");
            }

            if (string.IsNullOrWhiteSpace(User.Gender))
                ModelState.AddModelError("User.Gender", "Vui lòng chọn giới tính!");

            if (string.IsNullOrWhiteSpace(User.Password))
                ModelState.AddModelError("User.Password", "Mật khẩu không được để trống!");
        }

        private void ValidateTourist()
        {
            if (string.IsNullOrWhiteSpace(IdCard) || !Regex.IsMatch(IdCard, @"^0\d{11}$"))
                ModelState.AddModelError("IdCard", "Số CCCD phải bắt đầu bằng 0 và có 12 chữ số!");
            if (FrontIdcardFile == null && string.IsNullOrEmpty(ExistingFrontIdcard))
                ModelState.AddModelError("FrontIdcardFile", "Thiếu mặt trước CCCD!");
            if (BackIdcardFile == null && string.IsNullOrEmpty(ExistingBackIdcard))
                ModelState.AddModelError("BackIdcardFile", "Thiếu mặt sau CCCD!");
        }

        private void ValidateEmployee()
        {
            if (EmployeeCode == null || EmployeeCode <= 0)
                ModelState.AddModelError("EmployeeCode", "Mã nhân viên không hợp lệ!");
            if (string.IsNullOrEmpty(HireDateRaw))
                ModelState.AddModelError("HireDateRaw", "Thiếu ngày vào làm!");
        }
    }
}