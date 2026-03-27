using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Mail;
using TravelSystem.Models;

namespace TravelSystem.Pages.Auths
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public ForgotPasswordModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; }

        public string Error { get; set; }

        public IActionResult OnPost()
        {
            var user = _context.Users.FirstOrDefault(x => x.Gmail == Email);

            if (user == null)
            {
                Error = "Email không tồn tại!";
                return Page();
            }

            var otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("OTP_HASH", BCrypt.Net.BCrypt.HashPassword(otp));
            HttpContext.Session.SetString("OTP_EXPIRE", DateTime.Now.AddMinutes(2).ToString());
            HttpContext.Session.SetString("RESET_EMAIL", Email);

            SendEmail(Email, otp);

            return RedirectToPage("VerifyOTP");
        }

        private void SendEmail(string toEmail, string otp)
        {
            var fromEmail = "phucthcsvt@gmail.com";
            var password = "tuqd lxyo batr xftl";

            MailMessage mail = new MailMessage(fromEmail, toEmail);
            mail.Subject = "OTP Reset Password";
            mail.Body = $"OTP: {otp} (hết hạn 2 phút)";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(fromEmail, password);
            smtp.EnableSsl = true;

            smtp.Send(mail);
        }
    }
}
