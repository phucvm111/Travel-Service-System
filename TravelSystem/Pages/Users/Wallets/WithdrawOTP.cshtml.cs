using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Users.Wallets
{
    public class WithdrawOtpModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IEmailService _emailService;

        public WithdrawOtpModel(FinalPrnContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty] public string Digit1 { get; set; }
        [BindProperty] public string Digit2 { get; set; }
        [BindProperty] public string Digit3 { get; set; }
        [BindProperty] public string Digit4 { get; set; }
        [BindProperty] public string Digit5 { get; set; }
        [BindProperty] public string Digit6 { get; set; }

        public string ErrorMessage { get; set; }
        public long ExpiryTime { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Kiểm tra RoleID (Phải là 5 - User đã xác thực tài khoản mới được rút)
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 5)
            {
                TempData["ErrorMessage"] = "Vui lòng xác thực tài khoản để có thể sử dụng chức năng rút tiền!";
                // Chuyển hướng về trang cập nhật/xác thực thông tin của bạn
                return RedirectToPage("/Users/Profiles/UpdateTourist");
            }

            // 2. Tạo OTP 6 chữ số ngẫu nhiên
            string otp = new Random().Next(100000, 999999).ToString();
            long expiry = DateTimeOffset.Now.ToUnixTimeMilliseconds() + (5 * 60 * 1000); // 5 phút

            // Lưu vào Session để đối chiếu lúc OnPost
            HttpContext.Session.SetString("WithdrawOTP", otp);
            HttpContext.Session.SetString("OTPExpiry", expiry.ToString());
            HttpContext.Session.SetInt32("OTPFails", 0);

            // 3. Gửi OTP qua EmailService của bạn
            string userEmail = HttpContext.Session.GetString("UserEmail");
            string subject = "Go Viet - Xác thực mã OTP rút tiền";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee;'>
                    <h2 style='color: #28a745;'>Mã xác thực OTP</h2>
                    <p>Chào bạn, đây là mã xác thực để thực hiện yêu cầu rút tiền của bạn:</p>
                    <h1 style='letter-spacing: 5px; color: #333;'>{otp}</h1>
                    <p>Mã này có hiệu lực trong <b>5 phút</b>. Vui lòng không cung cấp mã này cho bất kỳ ai.</p>
                    <hr/>
                    <p style='font-size: 12px; color: #888;'>Hệ thống Travel System - Go Viet</p>
                </div>";

            await _emailService.SendAsync(userEmail, subject, body, true);

            ExpiryTime = expiry;
            return Page();
        }

        public IActionResult OnPost()
        {
            string inputOtp = Digit1 + Digit2 + Digit3 + Digit4 + Digit5 + Digit6;
            string storedOtp = HttpContext.Session.GetString("WithdrawOTP");
            string expiryStr = HttpContext.Session.GetString("OTPExpiry");

            // Ép kiểu an toàn
            long expiry = 0;
            if (!string.IsNullOrEmpty(expiryStr) && long.TryParse(expiryStr, out long parsedExpiry))
            {
                expiry = parsedExpiry;
            }

            ExpiryTime = expiry; // Để UI hiển thị tiếp bộ đếm nếu có lỗi khác
            int fails = HttpContext.Session.GetInt32("OTPFails") ?? 0;

            // Kiểm tra tồn tại và hết hạn (Nếu expiry = 0 nghĩa là Session đã mất)
            if (string.IsNullOrEmpty(storedOtp) || expiry == 0 || DateTimeOffset.Now.ToUnixTimeMilliseconds() > expiry)
            {
                ErrorMessage = "Mã OTP đã hết hạn hoặc không tồn tại. Vui lòng quay lại trang ví để nhận mã mới.";
                return Page();
            }

            if (inputOtp != storedOtp)
            {
                fails++;
                HttpContext.Session.SetInt32("OTPFails", fails);
                if (fails >= 3)
                {
                    HttpContext.Session.Remove("WithdrawOTP");
                    HttpContext.Session.Remove("OTPExpiry");
                    ErrorMessage = "Bạn đã nhập sai quá 3 lần. Vui lòng quay lại trang ví để nhận mã mới.";
                }
                else
                {
                    ErrorMessage = $"Mã OTP không chính xác. Bạn còn {3 - fails} lần thử.";
                }
                return Page();
            }

            // Xóa dấu vết OTP sau khi thành công
            HttpContext.Session.Remove("WithdrawOTP");
            HttpContext.Session.Remove("OTPExpiry");
            HttpContext.Session.Remove("OTPFails");

            // Đánh dấu đã xác thực thành công để trang Form cho phép truy cập
            HttpContext.Session.SetString("IsOtpVerified", "true");
            return RedirectToPage("/Users/Wallets/WithdrawForm");
        }
    }
}