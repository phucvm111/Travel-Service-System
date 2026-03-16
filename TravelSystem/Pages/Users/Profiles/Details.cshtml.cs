using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Profiles
{
    public class DetailsModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public DetailsModel(Prn222PrjContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User User { get; set; } = new User();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                id = HttpContext.Session.GetInt32("UserId");
            }

            if (id == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            User = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

            if (User == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}