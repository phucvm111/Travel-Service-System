using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Profiles
{
    public class EditModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hub;

        public EditModel(FinalPrnContext context, IHubContext<HubServer> hub)
        {
            _context = context;
            _hub = hub;
        }

        [BindProperty]
        public User User { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            User = await _context.Users.FirstOrDefaultAsync(m => m.UserId == id);

            if (User == null)
            {
                return NotFound();
            }

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ValidateInput();

            if (!ModelState.IsValid)
            {
                ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId");
                return Page();
            }

            var userInDb = await _context.Users.FindAsync(User.UserId);

            if (userInDb == null)
            {
                return NotFound();
            }

            userInDb.Gmail = User.Gmail?.Trim();
            userInDb.Password = User.Password?.Trim();
            userInDb.LastName = User.LastName?.Trim();
            userInDb.FirstName = User.FirstName?.Trim();
            userInDb.Dob = User.Dob;
            userInDb.Gender = User.Gender;
            userInDb.Address = User.Address?.Trim();
            userInDb.Phone = User.Phone?.Trim();
            userInDb.UpdateAt = DateOnly.FromDateTime(DateTime.Now);

            await _context.SaveChangesAsync();

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            return RedirectToPage("./Details", new { id = User.UserId });
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(User.LastName))
                ModelState.AddModelError("User.LastName", "Họ không được để trống!");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(User.LastName, @"^[\p{L} ]{2,50}$"))
                ModelState.AddModelError("User.LastName", "Họ chỉ chứa chữ cái và khoảng trắng (2-50 ký tự)");

            if (string.IsNullOrWhiteSpace(User.FirstName))
                ModelState.AddModelError("User.FirstName", "Tên không được để trống!");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(User.FirstName, @"^[\p{L} ]{2,50}$"))
                ModelState.AddModelError("User.FirstName", "Tên chỉ chứa chữ cái và khoảng trắng (2-50 ký tự)");

            if (string.IsNullOrWhiteSpace(User.Address))
                ModelState.AddModelError("User.Address", "Địa chỉ không được để trống!");
            else if (User.Address.Length < 5 || User.Address.Length > 200)
                ModelState.AddModelError("User.Address", "Địa chỉ phải từ 5 đến 200 ký tự!");

            if (string.IsNullOrWhiteSpace(User.Phone))
                ModelState.AddModelError("User.Phone", "Số điện thoại không được để trống!");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(User.Phone, @"^0\d{9}$"))
                ModelState.AddModelError("User.Phone", "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 chữ số!");

            DateOnly today = DateOnly.FromDateTime(DateTime.Now);

            if (User.Dob == null)
                ModelState.AddModelError("User.Dob", "Ngày sinh không được để trống!");
            else
            {
                DateOnly dob = User.Dob.Value;
                if (dob > today)
                    ModelState.AddModelError("User.Dob", "Ngày sinh không được lớn hơn hiện tại!");
                else if (dob.AddYears(18) > today)
                    ModelState.AddModelError("User.Dob", "Bạn phải đủ 18 tuổi!");
                else if (dob < today.AddYears(-100))
                    ModelState.AddModelError("User.Dob", "Tuổi không được lớn hơn 100!");
            }

            if (string.IsNullOrWhiteSpace(User.Gender))
                ModelState.AddModelError("User.Gender", "Vui lòng chọn giới tính!");
        }
    }
}