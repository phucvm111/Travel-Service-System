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
        private readonly FinalPrnContext _context;

        public DetailsModel(FinalPrnContext context)
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

            // Lấy thông tin User kèm theo danh sách Tourists liên quan
            User = await _context.Users
                .Include(u => u.Tourist)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (User == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}