using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Users.Wallets
{
    public class WithdrawFormModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IEmailService _emailService;

        public WithdrawFormModel(FinalPrnContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty] public decimal Amount { get; set; }
        [BindProperty] public string BankName { get; set; }
        [BindProperty] public string AccountNumber { get; set; }
        [BindProperty] public string AccountName { get; set; }
        [BindProperty] public string Phone { get; set; }
        [BindProperty] public string Cccd { get; set; }

        public decimal UserBalance { get; set; }
        public Tourist TouristInfo { get; set; }
        public string Mess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Bảo mật: Kiểm tra OTP
            if (HttpContext.Session.GetString("IsOtpVerified") != "true")
                return RedirectToPage("./WithdrawOtp");

            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var user = await _context.Users.FirstOrDefaultAsync(w => w.UserId == userId);
            TouristInfo = await _context.Tourists.FirstOrDefaultAsync(t => t.TouristId == userId);

            if (user == null || TouristInfo == null) return NotFound();

            UserBalance = user.Balance ?? 0;

            // Load sẵn dữ liệu cũ
            BankName = TouristInfo.BankName;
            AccountNumber = TouristInfo.BankNumber;
            AccountName = TouristInfo.AccountHolderName;
            Cccd = TouristInfo.IdCard;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var user = await _context.Users.FirstOrDefaultAsync(w => w.UserId == userId);
            var tourist = await _context.Tourists.FirstOrDefaultAsync(t => t.TouristId == userId);

            if (Amount < 10000 || Amount > (user?.Balance ?? 0))
            {
                Mess = "Số tiền không hợp lệ hoặc vượt quá số dư.";
                TouristInfo = tourist;
                UserBalance = user?.Balance ?? 0;
                return Page();
            }

            var request = new FundRequest
            {
                CreateBy = userId,
                Amount = Amount,
                RequestDate = DateTime.Now,
                Status = "PENDING",
                Type = "WITHDRAW",
                BankNameSnapshot = BankName,
                BankAccountSnapshot = AccountNumber,
                ReferenceCode = "WD" + DateTime.Now.Ticks.ToString().Substring(12)
            };

            _context.FundRequests.Add(request);
            await _context.SaveChangesAsync();

            // Gửi email
            string userEmail = HttpContext.Session.GetString("UserEmail");
            await _emailService.SendAsync(userEmail, "Yêu cầu rút tiền", $"Yêu cầu rút {Amount:N0} VNĐ thành công.", true);

            TempData["SuccessMessage"] = "Yêu cầu rút tiền của bạn đã được gửi thành công và đang chờ xử lý!";

            HttpContext.Session.Remove("IsOtpVerified");
            return RedirectToPage("./AmountToPay", new { success = 1 });
        }
    }
}