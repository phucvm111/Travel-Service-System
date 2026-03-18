using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public IndexModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public const int PageSize = 10;

        public List<RestaurantRow> Restaurants { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? agentId = HttpContext.Session.GetInt32("UserID");
            if (agentId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            IQueryable<RestaurantRow> query =
                from r in _context.Restaurants
                join s in _context.Services on r.ServiceId equals s.ServiceId
                where s.TravelAgentId == agentId
                      && (s.ServiceType == "RESTAURANT" || s.ServiceType == "RESTAURANT_INACTIVE")
                select new RestaurantRow
                {
                    RestaurantId = r.RestaurantId,
                    ServiceId = r.ServiceId,
                    Name = r.Name ?? string.Empty,
                    Image = r.Image,
                    Address = r.Address,
                    Phone = r.Phone,
                    TimeOpen = r.TimeOpen,
                    TimeClose = r.TimeClose,
                    ServiceType = s.ServiceType
                };

            if (!string.IsNullOrWhiteSpace(SearchName))
            {
                string keyword = SearchName.Trim();
                query = query.Where(r => r.Name.Contains(keyword));
            }

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (TotalPages > 0 && CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            Restaurants = await query
                .OrderBy(r => r.Name)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int restaurantId)
        {
            int? agentId = HttpContext.Session.GetInt32("UserID");
            if (agentId == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Service)
                .FirstOrDefaultAsync(r =>
                    r.RestaurantId == restaurantId &&
                    r.Service != null &&
                    r.Service.TravelAgentId == agentId);

            if (restaurant == null || restaurant.Service == null)
            {
                TempData["Error"] = "Không tìm thấy nhà hàng để cập nhật trạng thái.";
                return RedirectToPage();
            }

            restaurant.Service.ServiceType =
                restaurant.Service.ServiceType == "RESTAURANT"
                    ? "RESTAURANT_INACTIVE"
                    : "RESTAURANT";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thay đổi trạng thái nhà hàng thành công.";
            return RedirectToPage(new { currentPage = CurrentPage, searchName = SearchName });
        }

        public class RestaurantRow
        {
            public int RestaurantId { get; set; }

            public int ServiceId { get; set; }

            public string Name { get; set; } = string.Empty;

            public string? Image { get; set; }

            public string? Address { get; set; }

            public string? Phone { get; set; }

            public TimeOnly? TimeOpen { get; set; }

            public TimeOnly? TimeClose { get; set; }

            public string? ServiceType { get; set; }

            public bool IsActive => ServiceType == "RESTAURANT";

            public string StatusText => IsActive ? "Đang hoạt động" : "Dừng hoạt động";

            public string BadgeClass => IsActive ? "bg-success" : "bg-secondary";

            public string OperatingHours =>
                TimeOpen == null || TimeClose == null
                    ? "N/A"
                    : $"{TimeOpen:HH\\:mm} - {TimeClose:HH\\:mm}";
        }
    }
}