using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
{
    public class DetailModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public DetailModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public int RestaurantId { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Description { get; set; }
        public TimeOnly? TimeOpen { get; set; }
        public TimeOnly? TimeClose { get; set; }
        public string? ServiceType { get; set; }

        public bool IsActive => ServiceType == "RESTAURANT";

        public string StatusText => IsActive ? "Đang hoạt động" : "Dừng hoạt động";

        public string BadgeClass => IsActive ? "bg-success" : "bg-secondary";

        public string OperatingHours =>
            TimeOpen == null || TimeClose == null
                ? "Chưa cập nhật"
                : $"{TimeOpen:HH\\:mm} - {TimeClose:HH\\:mm}";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? agentId = HttpContext.Session.GetInt32("UserID");
            if (agentId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            var data = await (
                from r in _context.Restaurants
                join s in _context.Services on r.ServiceId equals s.ServiceId
                where r.RestaurantId == id && s.TravelAgentId == agentId
                select new
                {
                    r.RestaurantId,
                    r.ServiceId,
                    r.Name,
                    r.Image,
                    r.Address,
                    r.Phone,
                    r.Description,
                    r.TimeOpen,
                    r.TimeClose,
                    s.ServiceType
                }
            ).FirstOrDefaultAsync();

            if (data == null)
            {
                return NotFound();
            }

            RestaurantId = data.RestaurantId;
            ServiceId = data.ServiceId;
            Name = data.Name ?? string.Empty;
            Image = data.Image;
            Address = data.Address ?? string.Empty;
            Phone = data.Phone;
            Description = data.Description;
            TimeOpen = data.TimeOpen;
            TimeClose = data.TimeClose;
            ServiceType = data.ServiceType;

            return Page();
        }
    }
}