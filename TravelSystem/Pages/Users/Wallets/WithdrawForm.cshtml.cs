using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services; // Namespace ch?a IEmailService c?a b?n
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TravelSystem.Pages.Users.Wallets
{
    public class WithdrawFormModel : PageModel
    {
        private readonly FinalPrnContext _con;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;

        public WithdrawFormModel(FinalPrnContext con, IEmailService emailService, IWebHostEnvironment environment)
        {
            _con = con;
            _emailService = emailService;
            _environment = environment;
        }

        public decimal Balance { get; set; }

        [BindProperty]
        public string BankName { get; set; }
        [BindProperty]
        public string AccountNumber { get; set; }
        [BindProperty]
        public string AccountName { get; set; }
        [BindProperty]
        public string Phone { get; set; }
        [BindProperty]
        public string Cccd { get; set; }
        [BindProperty]
        public int Amount { get; set; }
        [BindProperty]
        public IFormFile CccdFront { get; set; }
        [BindProperty]
        public IFormFile CccdBack { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var wallet = _con.Wallets.FirstOrDefault(w => w.UserId == userId);
            Balance = wallet?.Balance ?? 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var user = _con.Users.FirstOrDefault(u => u.UserId == userId);
            var wallet = _con.Wallets.FirstOrDefault(w => w.UserId == userId);
            Balance = wallet?.Balance ?? 0;

            // 1. Validate Account Number
            if (!Regex.IsMatch(AccountNumber, "^[a-zA-Z0-9]{6,20}$"))
            {
                ErrorMessage = "Tài khoản không được có kí tự đặc biệt (6-20 ký tự).";
                return Page();
            }

            // 2. Validate Account Name
            // \p{L} đại diện cho bất kỳ ký tự chữ cái nào trong Unicode
            // \s đại diện cho khoảng trắng
            if (string.IsNullOrEmpty(AccountName) || !Regex.IsMatch(AccountName, @"^[\p{L}\s]{1,255}$"))
            {
                ErrorMessage = "Tên tài khoản không hợp lệ (không chứa ký tự đặc biệt hoặc số).";
                return Page();
            }

            // 3. Validate Phone
            if (!Regex.IsMatch(Phone, "^0\\d{9}$"))
            {
                ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số!";
                return Page();
            }

            // 4. Validate CCCD
            if (!Regex.IsMatch(Cccd, "^[0-9]{12}$"))
            {
                ErrorMessage = "Số CCCD phải gồm đúng 12 chữ số.";
                return Page();
            }

            // 5. Validate Amount
            if (Amount < 10000 || Amount > 100000000)
            {
                ErrorMessage = "Số tiền tối thiểu là 10,000 VNĐ và tối đa là 100 triệu VNĐ.";
                return Page();
            }

            if (Amount > Balance)
            {
                ErrorMessage = "Số dư không đủ để thực hiện yêu cầu này.";
                return Page();
            }

            // 6. X? lý Upload File
            if (CccdFront == null || CccdBack == null)
            {
                ErrorMessage = "Vui lòng tải lên ảnh mặt trước và mặt sau của cccd";
                return Page();
            }

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string frontFileName = $"{DateTime.Now.Ticks}_front_{userId}{Path.GetExtension(CccdFront.FileName)}";
            string backFileName = $"{DateTime.Now.Ticks}_back_{userId}{Path.GetExtension(CccdBack.FileName)}";

            string frontPath = Path.Combine(uploadsFolder, frontFileName);
            string backPath = Path.Combine(uploadsFolder, backFileName);

            using (var fileStream = new FileStream(frontPath, FileMode.Create)) await CccdFront.CopyToAsync(fileStream);
            using (var fileStream = new FileStream(backPath, FileMode.Create)) await CccdBack.CopyToAsync(fileStream);

            // 7. L?u vào Database
            try
            {
                var withdrawRequest = new WithdrawRequest
                {
                    UserId = userId.Value,
                    Amount = Amount,
                    Email = user.Gmail,
                    BankName = BankName,
                    BankAccountNumber = AccountNumber,
                    AccountHolderName = AccountName,
                    Phone = Phone,
                    IdCard = Cccd,
                    IdCardFrontImage = "uploads/" + frontFileName,
                    IdCardBackImage = "uploads/" + backFileName,
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                };

                _con.WithdrawRequests.Add(withdrawRequest);
                await _con.SaveChangesAsync();

                // 8. G?i Email thông báo
                string subject = "Xác nhận gửi yêu cầu rút tiền thành công.";
                string body = $@"
                    <h3>Chào {user.FirstName} {user.LastName},</h3>
                    <p>Bạn đã gửi yêu cầu rút tiền thành công.</p>
                    <ul>
                        <li><b>Số tiền:</b> {Amount:N0} VNĐ</li>
                        <li><b>Trạng thái:</b> Đang chờ duyệt</li>
                    </ul>
                    <p>Quý khách vui lòng đợi thông tin sử lý từ 1-3 ngày làm việc.</p>
                    <p>Trân trọng,<br>Go Viet</p>";

                await _emailService.SendAsync(user.Gmail, subject, body, true);

                TempData["SuccessMessage"] = $"Gửi yêu cầu rút tiền thành công! Số tiền {Amount:N0} VNĐ đang được chờ duyệt.";

                return RedirectToPage("/Users/Wallets/WalletAmount", new { success = 1 });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Có lỗi xảy ra: " + ex.Message;
                return Page();
            }
        }
    }
}