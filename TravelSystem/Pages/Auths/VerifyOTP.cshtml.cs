using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;

namespace TravelSystem.Pages.Auths
{
    public class VerifyOTPModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public VerifyOTPModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string OTP { get; set; }

        public string Error { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            // Kiểm tra nếu chưa có email trong session thì quay về trang quên mật khẩu
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("RESET_EMAIL")))
            {
                Response.Redirect("ForgotPassword");
            }
        }

        public IActionResult OnPost()
        {
            var hash = HttpContext.Session.GetString("OTP_HASH");
            var expire = HttpContext.Session.GetString("OTP_EXPIRE");

            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(expire))
            {
                Error = "Mã OTP không tồn tại hoặc đã bị xóa!";
                return Page();
            }

            if (DateTime.Now > DateTime.Parse(expire))
            {
                Error = "Mã OTP đã hết hạn! Vui lòng bấm gửi lại.";
                return Page();
            }

            // Kiểm tra OTP người dùng nhập vào
            if (!BCrypt.Net.BCrypt.Verify(OTP, hash))
            {
                Error = "Mã OTP không chính xác!";
                return Page();
            }

            // Xác thực thành công
            HttpContext.Session.SetString("OTP_VERIFIED", "true");
            return RedirectToPage("ResetPassword");
        }

        public IActionResult OnPostResend()
        {
            var email = HttpContext.Session.GetString("RESET_EMAIL");
            if (string.IsNullOrEmpty(email)) return RedirectToPage("ForgotPassword");

            // 1. Tạo OTP mới
            var otpCode = new Random().Next(100000, 999999).ToString();

            // 2. Lưu vào Session mới
            HttpContext.Session.SetString("OTP_HASH", BCrypt.Net.BCrypt.HashPassword(otpCode));
            HttpContext.Session.SetString("OTP_EXPIRE", DateTime.Now.AddMinutes(2).ToString());

            // 3. GỌI HÀM GỬI MAIL CỦA BẠN TẠI ĐÂY
            bool isSent = SendEmailLogic(email, otpCode);

            if (isSent)
            {
                SuccessMessage = "Mã OTP mới đã được gửi thành công!";
            }
            else
            {
                Error = "Lỗi gửi email. Vui lòng thử lại sau.";
            }

            return Page();
        }

        private bool SendEmailLogic(string email, string otp)
        {
            try
            {
                // COPY LOGIC GỬI MAIL (SmtpClient...) TỪ TRANG FORGOTPASSWORD QUA ĐÂY
                // Ví dụ: _emailService.Send(email, "Mã xác thực mới", $"Mã của bạn là: {otp}");
                return true;
            }
            catch { return false; }
        }
    }
}