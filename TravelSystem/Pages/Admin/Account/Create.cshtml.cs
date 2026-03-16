using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Account
{
    public class CreateModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IHubContext<HubServer> _hubContext;

        public CreateModel(Prn222PrjContext context, IHubContext<HubServer> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [BindProperty] public string Gmail { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";
        [BindProperty] public string RePassword { get; set; } = "";
        [BindProperty] public string LastName { get; set; } = "";
        [BindProperty] public string FirstName { get; set; } = "";
        [BindProperty] public DateOnly? Dob { get; set; }
        [BindProperty] public string Gender { get; set; } = "";
        [BindProperty] public string Address { get; set; } = "";
        [BindProperty] public string Phone { get; set; } = "";

        public Dictionary<string, string> Errors { get; set; } = new();

        public IActionResult OnGet()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null) return RedirectToPage("/Auths/Login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null) return RedirectToPage("/Auths/Login");

            ValidateInput();

            if (Errors.Count > 0) return Page();

            var now = DateOnly.FromDateTime(DateTime.Now);

            var newAdmin = new User
            {
                Gmail = Gmail.Trim(),
                Password = Password.Trim(),
                LastName = LastName.Trim(),
                FirstName = FirstName.Trim(),
                Dob = Dob,
                Gender = Gender,
                Address = Address.Trim(),
                Phone = Phone.Trim(),
                CreateAt = now,
                UpdateAt = now,
                Status = 1,
                RoleId = 1
            };

            _context.Users.Add(newAdmin);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("loadAll");

            TempData["SuccessMessage"] = "Thêm quản trị viên thành công!";
            return RedirectToPage("/Admin/Account/Index");
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(LastName))
                Errors["LastName"] = "Họ không được để trống!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(LastName.Trim(), @"^[\p{L} ]{2,50}$"))
                Errors["LastName"] = "Họ chỉ được chứa chữ cái và khoảng trắng, độ dài từ 2 đến 50 ký tự!";

            if (string.IsNullOrWhiteSpace(FirstName))
                Errors["FirstName"] = "Tên không được để trống!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(FirstName.Trim(), @"^[\p{L} ]{2,50}$"))
                Errors["FirstName"] = "Tên chỉ được chứa chữ cái và khoảng trắng, độ dài từ 2 đến 50 ký tự!";

            if (string.IsNullOrWhiteSpace(Address))
                Errors["Address"] = "Địa chỉ không được để trống!";
            else if (Address.Trim().Length < 5 || Address.Trim().Length > 200)
                Errors["Address"] = "Địa chỉ phải từ 5 đến 200 ký tự!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(Address.Trim(), @"^[\p{L}\d\s,.]+$"))
                Errors["Address"] = "Địa chỉ chỉ được chứa chữ cái, số, khoảng trắng, dấu phẩy và dấu chấm!";

            if (string.IsNullOrWhiteSpace(Password))
                Errors["Password"] = "Mật khẩu không được để trống!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(Password, @"^[A-Z](?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-={}:"";'<>?,./]).{7,}$"))
                Errors["Password"] = "Mật khẩu phải bắt đầu bằng chữ hoa, chứa ít nhất 1 chữ thường, 1 số, 1 ký tự đặc biệt và có ít nhất 8 ký tự!";

            if (string.IsNullOrWhiteSpace(RePassword))
                Errors["RePassword"] = "Vui lòng nhập lại mật khẩu!";
            else if (Password != RePassword)
                Errors["RePassword"] = "Mật khẩu không khớp!";

            if (string.IsNullOrWhiteSpace(Phone))
                Errors["Phone"] = "Số điện thoại không được để trống!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(Phone.Trim(), @"^0\d{9}$"))
                Errors["Phone"] = "Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số!";

            if (Dob == null)
                Errors["Dob"] = "Ngày sinh không được để trống!";
            else
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (Dob > today)
                    Errors["Dob"] = "Ngày sinh không được lớn hơn ngày hiện tại!";
                else if (Dob.Value.AddYears(18) > today)
                    Errors["Dob"] = "Quản trị viên phải đủ 18 tuổi trở lên!";
                else if (Dob.Value.AddYears(100) < today)
                    Errors["Dob"] = "Tuổi không được lớn hơn 100!";
            }

            if (string.IsNullOrWhiteSpace(Gender))
                Errors["Gender"] = "Vui lòng chọn giới tính!";

            if (string.IsNullOrWhiteSpace(Gmail))
                Errors["Gmail"] = "Gmail không được để trống!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(Gmail.Trim(), @"^[A-Za-z0-9+_.-]+@(.+)$"))
                Errors["Gmail"] = "Gmail không hợp lệ!";
            else if (_context.Users.Any(u => u.Gmail == Gmail.Trim()))
                Errors["Gmail"] = "Gmail đã được đăng ký!";
        }
    }
}
