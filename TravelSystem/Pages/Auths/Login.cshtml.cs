using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;

namespace TravelSystem.Pages.Auths
{
    public class LoginModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public LoginModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Gmail { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Gmail) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin!";
                return Page();
            }

            var user = _context.Users
                .FirstOrDefault(u => u.Gmail == Gmail && u.Password == Password);

            if (user == null)
            {
                ErrorMessage = "Email hoặc mật khẩu không đúng!";
                return Page();
            }

            // lưu session
            HttpContext.Session.SetString("UserEmail", Gmail);
            HttpContext.Session.SetInt32("UserID", user.UserId);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);
            return RedirectToPage("/Index");
            
        }
    }
}