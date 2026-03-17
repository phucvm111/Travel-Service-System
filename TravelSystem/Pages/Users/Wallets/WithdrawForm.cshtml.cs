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
        private readonly Prn222PrjContext _con;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;

        public WithdrawFormModel(Prn222PrjContext con, IEmailService emailService, IWebHostEnvironment environment)
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
                ErrorMessage = "Tài kho?n không ???c có kí t? ??c bi?t (6-20 ký t?).";
                return Page();
            }

            // 2. Validate Account Name
            if (string.IsNullOrEmpty(AccountName) || !Regex.IsMatch(AccountName, @"^[a-zA-ZÀ-?\s]{1,255}$"))
            {
                ErrorMessage = "Tên tài kho?n không h?p l? (không ch?a ký t? ??c bi?t).";
                return Page();
            }

            // 3. Validate Phone
            if (!Regex.IsMatch(Phone, "^0\\d{9}$"))
            {
                ErrorMessage = "S? ?i?n tho?i ph?i b?t ??u b?ng s? 0 và có ?úng 10 ch? s?!";
                return Page();
            }

            // 4. Validate CCCD
            if (!Regex.IsMatch(Cccd, "^[0-9]{12}$"))
            {
                ErrorMessage = "S? CCCD ph?i g?m ?úng 12 ch? s?.";
                return Page();
            }

            // 5. Validate Amount
            if (Amount < 10000 || Amount > 100000000)
            {
                ErrorMessage = "S? ti?n t?i thi?u là 10,000 VN? và t?i ?a là 100 tri?u VN?.";
                return Page();
            }

            if (Amount > Balance)
            {
                ErrorMessage = "S? d? không ?? ?? th?c hi?n yêu c?u này.";
                return Page();
            }

            // 6. X? lý Upload File
            if (CccdFront == null || CccdBack == null)
            {
                ErrorMessage = "Vui lòng t?i lên ?nh m?t tr??c và m?t sau c?a CCCD.";
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
                string subject = "Xác nh?n g?i yêu c?u rút ti?n thành công.";
                string body = $@"
                    <h3>Chào {user.FirstName} {user.LastName},</h3>
                    <p>B?n ?ã g?i yêu c?u rút ti?n thành công.</p>
                    <ul>
                        <li><b>S? ti?n:</b> {Amount:N0} VN?</li>
                        <li><b>Tr?ng thái:</b> ?ang ch? duy?t</li>
                    </ul>
                    <p>Quý khách vui lòng ??i thông tin x? lý t? 1-3 ngày làm vi?c.</p>
                    <p>Trân tr?ng,<br>??i ng? Go Viet</p>";

                await _emailService.SendAsync(user.Gmail, subject, body, true);

                return RedirectToPage("/Users/Wallets/WalletAmount", new { success = 1 });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Có l?i x?y ra: " + ex.Message;
                return Page();
            }
        }
    }
}