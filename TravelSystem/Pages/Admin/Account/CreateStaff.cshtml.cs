using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Account
{
    public class CreateStaffModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hubContext;

        public CreateStaffModel(FinalPrnContext context, IHubContext<HubServer> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // BindProperty giúp giữ lại giá trị trên Form sau khi Post
        [BindProperty] public string Gmail { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";
        [BindProperty] public string RePassword { get; set; } = "";
        [BindProperty] public string LastName { get; set; } = "";
        [BindProperty] public string FirstName { get; set; } = "";
        [BindProperty] public DateOnly? Dob { get; set; }
        [BindProperty] public string Gender { get; set; } = "";
        [BindProperty] public string Address { get; set; } = "";
        [BindProperty] public string Phone { get; set; } = "";
        [BindProperty] public int? EmployeeCode { get; set; }
        [BindProperty] public DateOnly? HireDate { get; set; }

        public Dictionary<string, string> Errors { get; set; } = new();

        public IActionResult OnGet()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null) return RedirectToPage("/Auths/Login");

            HireDate = DateOnly.FromDateTime(DateTime.Now);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ValidateInput();

            if (Errors.Count > 0) return Page();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var now = DateOnly.FromDateTime(DateTime.Now);

                    // 1. Tạo User
                    var newUser = new User
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
                        RoleId = 4 // Giả định RoleId 4 là Staff
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // 2. Tạo Staff
                    var newStaff = new Staff
                    {
                        StaffId = newUser.UserId,
                        EmployeeCode = EmployeeCode.Value,
                        HireDate = HireDate,
                        WorkStatus = "Active"
                    };

                    _context.Staff.Add(newStaff);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // 3. SignalR thông báo cập nhật danh sách
                    await _hubContext.Clients.All.SendAsync("loadAll");

                    TempData["SuccessMessage"] = "Thêm nhân viên thành công!";
                    return RedirectToPage("/Admin/Account/Index");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    Errors["System"] = "Có lỗi xảy ra trong quá trình lưu dữ liệu!";
                    return Page();
                }
            }
        }

        private void ValidateInput()
        {
            // --- Validate User (Giữ nguyên từ Create Admin) ---
            if (string.IsNullOrWhiteSpace(LastName)) Errors["LastName"] = "Họ không được để trống!";
            if (string.IsNullOrWhiteSpace(FirstName)) Errors["FirstName"] = "Tên không được để trống!";

            if (string.IsNullOrWhiteSpace(Password)) Errors["Password"] = "Mật khẩu không được để trống!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(Password, @"^[A-Z](?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-={}:"";'<>?,./]).{7,}$"))
                Errors["Password"] = "Mật khẩu phải bắt đầu bằng chữ hoa, chứa ít nhất 1 chữ thường, 1 số, 1 ký tự đặc biệt!";

            if (Password != RePassword) Errors["RePassword"] = "Mật khẩu không khớp!";

            if (string.IsNullOrWhiteSpace(Phone)) Errors["Phone"] = "Số điện thoại không được để trống!";
            else if (!System.Text.RegularExpressions.Regex.IsMatch(Phone.Trim(), @"^0\d{9}$"))
                Errors["Phone"] = "Số điện thoại phải có 10 chữ số!";

            if (Dob == null) Errors["Dob"] = "Vui lòng chọn ngày sinh!";

            if (string.IsNullOrWhiteSpace(Gmail)) Errors["Gmail"] = "Gmail không được để trống!";
            else if (_context.Users.Any(u => u.Gmail == Gmail.Trim()))
                Errors["Gmail"] = "Gmail đã được đăng ký!";

            // --- Validate Staff fields ---
            if (EmployeeCode == null || EmployeeCode <= 0)
                Errors["EmployeeCode"] = "Mã nhân viên phải là số dương!";
            else if (_context.Staff.Any(s => s.EmployeeCode == EmployeeCode))
                Errors["EmployeeCode"] = "Mã nhân viên này đã tồn tại!";

            if (HireDate == null)
                Errors["HireDate"] = "Vui lòng chọn ngày vào làm!";
        }
    }
}