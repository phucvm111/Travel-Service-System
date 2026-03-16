using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Accommodations
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public IndexModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public IList<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
        public HashSet<int> AccommodationHasRooms { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        public int TotalPages { get; set; }
        private const int PageSize = 8;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (agent == null) return RedirectToPage("/Error");

            var query = _context.Accommodations
                .Include(a => a.Service)
                .Where(a => a.Service != null
                         && a.Service.TravelAgentId == agent.TravelAgentId
                         && a.Service.ServiceType == "ACCOMMODATION")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchName))
            {
                var keyword = SearchName.Trim();
                query = query.Where(a => a.Name != null && a.Name.Contains(keyword));
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (TotalPages == 0)
                TotalPages = 1;

            if (PageNumber < 1)
                PageNumber = 1;

            if (PageNumber > TotalPages)
                PageNumber = TotalPages;

            Accommodations = await query
                .OrderByDescending(a => a.AccommodationId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var accommodationIds = Accommodations.Select(a => a.AccommodationId).ToList();

            AccommodationHasRooms = (await _context.Rooms
                .Where(r => r.AccommodationId != null && accommodationIds.Contains(r.AccommodationId.Value))
                .Select(r => r.AccommodationId!.Value)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int pageNumber, string? searchName)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (agent == null) return RedirectToPage("/Error");

            var accommodation = await _context.Accommodations
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.AccommodationId == id
                    && a.Service != null
                    && a.Service.TravelAgentId == agent.TravelAgentId
                    && a.Service.ServiceType == "ACCOMMODATION");

            if (accommodation == null) return NotFound();

            bool hasRoom = await _context.Rooms.AnyAsync(r => r.AccommodationId == id);
            if (hasRoom)
            {
                TempData["ErrorMessage"] = "Không thể xóa khách sạn đã có phòng!";
                return RedirectToPage("./Index", new { PageNumber = pageNumber, SearchName = searchName });
            }

            bool usedInTour = await _context.TourServiceDetails.AnyAsync(t => t.ServiceId == accommodation.ServiceId);
            if (usedInTour)
            {
                TempData["ErrorMessage"] = "Không thể xóa khách sạn vì đang được sử dụng trong tour!";
                return RedirectToPage("./Index", new { PageNumber = pageNumber, SearchName = searchName });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceId == accommodation.ServiceId);

                _context.Accommodations.Remove(accommodation);

                if (service != null)
                {
                    _context.Services.Remove(service);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Xóa khách sạn thành công!";
                return RedirectToPage("./Index", new { PageNumber = pageNumber, SearchName = searchName });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa khách sạn!";
                return RedirectToPage("./Index", new { PageNumber = pageNumber, SearchName = searchName });
            }
        }
    }
}