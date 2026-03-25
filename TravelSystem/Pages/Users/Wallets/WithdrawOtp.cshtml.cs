using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Users.Wallets
{
    public class WithdrawOtpModel : PageModel
    {
        private readonly FinalPrnContext _con;
        private readonly IEmailService _emailService; // Inject service của bạn

        public WithdrawOtpModel(FinalPrnContext con, IEmailService emailService)
        {
            _con = con;
            _emailService = emailService;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public int? RemainingTries { get; set; }

        public long OtpExpiry { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var loginUser = _con.Users.FirstOrDefault(u => u.UserId == userId);
            if (loginUser == null) return RedirectToPage("/Auths/Login");

            // Tạo OTP 6 chữ số
            string otp = new Random().Next(0, 1000000).ToString("D6");
            long expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds();

            // Lưu vào Session
            HttpContext.Session.SetString("otp", otp);
            HttpContext.Session.SetInt64("otpExpiry", expiry);
            HttpContext.Session.SetInt32("otpFails", 0);

            // GỌI SERVICE CỦA BẠN ĐỂ GỬI MAIL
            string subject = "Go Viet gửi mã OTP.";
            string body = $@"
                <h3>Chào {loginUser.Gmail},</h3>
                <p>Mã OTP của bạn là: <b style='font-size: 20px; color: #198754;'>{otp}</b></p>
                <p>Mã này có hiệu lực trong 5 phút. Vui lòng không cung cấp mã này cho bất kỳ ai!</p>
                <p>Trân trọng,<br>Đội ngũ Go Viet</p>";

            await _emailService.SendAsync(loginUser.Gmail!, subject, body, true);

            OtpExpiry = expiry;
            return Page();
        }

        public IActionResult OnPost(List<string> Digits)
        {
            string inputOtp = string.Join("", Digits);
            string otpStored = HttpContext.Session.GetString("otp");
            long? expiryTime = HttpContext.Session.GetInt64("otpExpiry");
            int? failCount = HttpContext.Session.GetInt32("otpFails") ?? 0;

            OtpExpiry = expiryTime ?? 0;

            // Kiểm tra hết hạn
            if (string.IsNullOrEmpty(otpStored) || expiryTime == null || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > expiryTime)
            {
                ClearOtpSession();
                ErrorMessage = "Mã OTP đã hết hạn hoặc không tồn tại. Vui lòng gửi lại mã mới.";
                return Page();
            }

            // Kiểm tra đúng/sai
            if (inputOtp != otpStored)
            {
                failCount++;
                HttpContext.Session.SetInt32("otpFails", failCount.Value);

                if (failCount >= 3)
                {
                    ClearOtpSession();
                    ErrorMessage = "Bạn đã nhập sai quá 3 lần. Vui lòng yêu cầu gửi lại mã OTP.";
                }
                else
                {
                    ErrorMessage = "Mã OTP không đúng.";
                    RemainingTries = 3 - failCount;
                }
                return Page();
            }

            // Đúng OTP
            ClearOtpSession();
            return RedirectToPage("/Users/Wallets/WithdrawForm");
        }

        public async Task<IActionResult> OnGetResendAsync()
        {
            return await OnGetAsync();
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove("otp");
            HttpContext.Session.Remove("otpExpiry");
            HttpContext.Session.Remove("otpFails");
        }
    }

    // Đảm bảo bạn có Helper này để đọc long từ Session
    public static class SessionExtensions
    {
        public static void SetInt64(this ISession session, string key, long value)
            => session.Set(key, BitConverter.GetBytes(value));

        public static long? GetInt64(this ISession session, string key)
            => session.TryGetValue(key, out var data) ? BitConverter.ToInt64(data) : null;
    }
}