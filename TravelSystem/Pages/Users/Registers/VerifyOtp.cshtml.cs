using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TravelSystem.Pages.Users.Registers
{
    public class VerifyOtpModel : PageModel
    {
        [BindProperty]
        public string InputOtp { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnPost()
        {
            var otp = HttpContext.Session.GetString("otp");
            var expiryStr = HttpContext.Session.GetString("otpExpiry");

            if (otp == null)
            {
                ErrorMessage = "OTP không tồn tại";
                return Page();
            }

            DateTime expiry = DateTime.Parse(expiryStr);

            if (DateTime.Now > expiry)
            {
                ErrorMessage = "OTP đã hết hạn";
                return Page();
            }

            if (InputOtp != otp)
            {
                ErrorMessage = "OTP không đúng";
                return Page();
            }

            return RedirectToPage("RegisterUser");
        }
    }
}