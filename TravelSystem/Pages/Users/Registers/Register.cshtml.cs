using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Users.Registers
{
    public class RegisterModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly Prn222PrjContext _context;

        public RegisterModel(IEmailService emailService, Prn222PrjContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        [BindProperty]
        public string Gmail { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var error = ValidateEmail(Gmail);
            if (error != null)
            {
                ErrorMessage = error;
                return Page();
            }

            Random rand = new Random();
            int otp = rand.Next(100000, 999999);

            HttpContext.Session.SetString("gmail", Gmail);
            HttpContext.Session.SetString("otp", otp.ToString());
            HttpContext.Session.SetString("otpExpiry", DateTime.Now.AddMinutes(2).ToString());

            await _emailService.SendAsync(
                Gmail,
                "Mã xác nhận đăng ký - GoViet",
                $"<h3>Mã OTP của bạn là: <b>{otp}</b></h3><p>Mã có hiệu lực trong 2 phút.</p>",
                true
            );

            return RedirectToPage("VerifyOtp");
        }

        private string ValidateEmail(string gmail)
        {
            if (string.IsNullOrWhiteSpace(gmail))
                return "Email không được để trống!";

            var pattern = @"^[a-zA-Z0-9._%+-]+@(gmail\.com|[a-zA-Z0-9.-]+\.(com|net|org|vn|edu|edu\.vn|ac\.vn))$";
            if (!Regex.IsMatch(gmail.Trim(), pattern, RegexOptions.IgnoreCase))
                return "Chỉ chấp nhận email hợp lệ như @gmail.com, @abc.edu, @company.com...";

            if (_context.Users.Any(u => u.Gmail.ToLower() == gmail.Trim().ToLower()))
                return "Gmail đã được sử dụng. Vui lòng dùng gmail khác.";

            return null;
        }
    }
}
