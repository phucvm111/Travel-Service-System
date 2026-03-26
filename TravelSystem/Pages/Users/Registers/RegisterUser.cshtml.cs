using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.RegularExpressions;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Registers
{
    public class RegisterUserModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hub;

        public RegisterUserModel(FinalPrnContext context, IHubContext<HubServer> hub)
        {
            _context = context;
            _hub = hub;
        }

        [BindProperty]
        public UserRegisterInput User { get; set; } = new();

        [BindProperty]
        public string? Repassword { get; set; }

        [TempData] public string? GeneralError { get; set; }
        [TempData] public bool RegisterSuccess { get; set; }

        public IActionResult OnGet()
        {
            var pendingJson = HttpContext.Session.GetString("pendingUser");
            if (!string.IsNullOrEmpty(pendingJson))
            {
                try
                {
                    var p = JsonSerializer.Deserialize<JsonElement>(pendingJson);
                    User.LastName = p.TryGetProperty("LastName", out var ln) ? ln.GetString() ?? "" : "";
                    User.FirstName = p.TryGetProperty("FirstName", out var fn) ? fn.GetString() ?? "" : "";
                    User.Phone = p.TryGetProperty("Phone", out var ph) ? ph.GetString() ?? "" : "";
                    User.Gender = p.TryGetProperty("Gender", out var ge) ? ge.GetString() ?? "" : "";
                    User.Gmail = p.TryGetProperty("Gmail", out var gm) ? gm.GetString() ?? "" : "";
                    User.Address = p.TryGetProperty("Address", out var ad) ? ad.GetString() ?? "" : "";
                    User.Password = p.TryGetProperty("Password", out var pw) ? pw.GetString() ?? "" : "";
                    if (p.TryGetProperty("Dob", out var db) && DateOnly.TryParse(db.GetString(), out var dob))
                        User.Dob = dob;
                }
                catch { }
            }
            else
            {
                User.Gmail = HttpContext.Session.GetString("gmail") ?? "";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? action)
        {
            // Xóa tất cả lỗi do model binding tự sinh (tiếng Anh)
            // rồi validate thủ công bằng tiếng Việt
            ModelState.Clear();
            ValidateUserInput();

            if (!ModelState.IsValid)
                return Page();

            var pendingData = new
            {
                User.LastName,
                User.FirstName,
                User.Phone,
                User.Gender,
                User.Gmail,
                User.Address,
                User.Password,
                Dob = User.Dob?.ToString("yyyy-MM-dd")
            };
            HttpContext.Session.SetString("pendingUser", JsonSerializer.Serialize(pendingData));

            if (action == "becomeAgent")
                return RedirectToPage("/Agents/AgentRegisters/AgentRegister");

            try
            {
                if (_context.Users.Any(u => u.Gmail == User.Gmail))
                {
                    ModelState.AddModelError("User.Gmail", "Gmail này đã được sử dụng!");
                    return Page();
                }

                var user = new User
                {
                    LastName = User.LastName,
                    FirstName = User.FirstName,
                    Phone = User.Phone,
                    Gender = User.Gender,
                    Gmail = User.Gmail,
                    Address = User.Address,
                    Password = User.Password,
                    Dob = User.Dob,
                    CreateAt = DateOnly.FromDateTime(DateTime.Now),
                    Status = 1,
                    RoleId = 2
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _ = _hub.Clients.All.SendAsync("loadAll");
            }
            catch (Exception ex)
            {
                GeneralError = "Đăng ký thất bại: " + ex.Message;
                return RedirectToPage();
            }

            HttpContext.Session.Remove("pendingUser");
            HttpContext.Session.Remove("gmail");
            HttpContext.Session.Remove("otp");
            HttpContext.Session.Remove("otpExpiry");

            RegisterSuccess = true;
            return RedirectToPage();
        }

        private void ValidateUserInput()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (string.IsNullOrWhiteSpace(User.LastName))
                ModelState.AddModelError("User.LastName", "Họ không được để trống!");
            else if (!Regex.IsMatch(User.LastName.Trim(), @"^[\p{L} ]{2,50}$"))
                ModelState.AddModelError("User.LastName", "Họ chỉ chứa chữ cái và khoảng trắng (2-50 ký tự)!");

            if (string.IsNullOrWhiteSpace(User.FirstName))
                ModelState.AddModelError("User.FirstName", "Tên không được để trống!");
            else if (!Regex.IsMatch(User.FirstName.Trim(), @"^[\p{L} ]{2,50}$"))
                ModelState.AddModelError("User.FirstName", "Tên chỉ chứa chữ cái và khoảng trắng (2-50 ký tự)!");

            if (string.IsNullOrWhiteSpace(User.Password))
                ModelState.AddModelError("User.Password", "Mật khẩu không được để trống!");
            else if (User.Password.Length < 6)
                ModelState.AddModelError("User.Password", "Mật khẩu phải có ít nhất 6 ký tự!");

            if (string.IsNullOrWhiteSpace(Repassword))
                ModelState.AddModelError("Repassword", "Vui lòng nhập lại mật khẩu!");
            else if (User.Password != Repassword)
                ModelState.AddModelError("Repassword", "Mật khẩu nhập lại không khớp!");

            if (string.IsNullOrWhiteSpace(User.Phone))
                ModelState.AddModelError("User.Phone", "Số điện thoại không được để trống!");
            else if (!Regex.IsMatch(User.Phone, @"^0\d{9}$"))
                ModelState.AddModelError("User.Phone", "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 chữ số!");

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

            if (string.IsNullOrWhiteSpace(User.Address))
                ModelState.AddModelError("User.Address", "Địa chỉ không được để trống!");
            else if (User.Address.Trim().Length < 5 || User.Address.Trim().Length > 200)
                ModelState.AddModelError("User.Address", "Địa chỉ phải từ 5 đến 200 ký tự!");
        }
    }

    public class UserRegisterInput
    {
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string Password { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Gmail { get; set; } = "";
        public string Address { get; set; } = "";
        public DateOnly? Dob { get; set; }
    }
}