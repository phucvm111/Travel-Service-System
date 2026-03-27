using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using TravelSystem.Models;

namespace TravelSystem.Pages.Auths
{
    public class ResetPasswordModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public ResetPasswordModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string NewPassword { get; set; }

        public string Error { get; set; }

        public IActionResult OnPost()
        {
            if (HttpContext.Session.GetString("OTP_VERIFIED") != "true")
                return RedirectToPage("Login");

            if (!Regex.IsMatch(NewPassword,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$"))
            {
                Error = "Mật khẩu >=8 ký tự, có chữ hoa, thường, số, ký tự đặc biệt!";
                return Page();
            }

            var email = HttpContext.Session.GetString("RESET_EMAIL");
            var user = _context.Users.FirstOrDefault(x => x.Gmail == email);

            //user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            user.Password = NewPassword;

            _context.SaveChanges();

            HttpContext.Session.Clear();

            return RedirectToPage("Login");
        }
    }
}
