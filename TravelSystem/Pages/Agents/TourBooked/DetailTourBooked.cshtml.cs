using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using System.Text.RegularExpressions;

namespace TravelSystem.Pages.Agents.TourBooked
{
    public class DetailTourBookedModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public DetailTourBookedModel(FinalPrnContext context) => _context = context;

        public Booking BookingItem { get; set; }
        public string CustomerNote { get; set; } = "Không có ghi chú nào.";
        public List<string> CancelLogs { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 3) return RedirectToPage("/Auths/Login");

            BookingItem = await _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .Include(b => b.Voucher)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (BookingItem == null) return NotFound();

            // Logic tách Ghi chú và Lý do hủy
            if (!string.IsNullOrEmpty(BookingItem.Note))
            {
                // Tách các đoạn phân cách bởi dấu gạch đứng |
                var parts = BookingItem.Note.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string content = part.Trim();
                    // Nếu chứa ngoặc vuông [ ] thì coi là log hệ thống/lý do hủy
                    if (Regex.IsMatch(content, @"\[.*?\]"))
                    {
                        CancelLogs.Add(content);
                    }
                    else if (!string.IsNullOrWhiteSpace(content))
                    {
                        CustomerNote = content;
                    }
                }
            }

            return Page();
        }
    }
}