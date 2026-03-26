using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public IndexModel(FinalPrnContext context)
        {
            _context = context;
        }

        public IList<Restaurant> Restaurants { get; set; } = new List<Restaurant>();

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 8;
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (agent == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đại lý.";
                return RedirectToPage("/Error");
            }

            var query = _context.Restaurants
                .AsNoTracking()
                .Include(x => x.Service)
                .Where(x => x.Service != null
                            && x.Service.AgentId == agent.TravelAgentId
                            && x.Service.ServiceType == 3);

            if (!string.IsNullOrWhiteSpace(SearchName))
            {
                var keyword = SearchName.Trim();
                query = query.Where(x => x.Service != null
                                         && x.Service.ServiceName != null
                                         && x.Service.ServiceName.Contains(keyword));
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(x => x.Service != null
                                         && x.Service.Status == StatusFilter.Value);
            }

            var totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (TotalPages == 0) TotalPages = 1;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            Restaurants = await query
                .OrderByDescending(x => x.ServiceId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }
    }
}