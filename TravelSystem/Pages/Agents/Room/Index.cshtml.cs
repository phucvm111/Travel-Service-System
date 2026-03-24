using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Room
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public IndexModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public IList<TravelSystem.Models.Room> RoomList { get; set; } = new List<TravelSystem.Models.Room>();

        [BindProperty(SupportsGet = true)]
        public string? SearchRoomType { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterAccommodationId { get; set; }

        public List<SelectListItem> AccommodationOptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            AccommodationOptions = await _context.Accommodations
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem
                {
                    Value = a.AccommodationId.ToString(),
                    Text = a.Name
                })
                .ToListAsync();

            IQueryable<TravelSystem.Models.Room> query = _context.Rooms
                .Include(r => r.Accommodation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchRoomType))
            {
                query = query.Where(r => r.RoomTypes != null && r.RoomTypes.Contains(SearchRoomType));
            }

            if (FilterAccommodationId.HasValue)
            {
                query = query.Where(r => r.AccommodationId == FilterAccommodationId.Value);
            }

            RoomList = await query
                .OrderBy(r => r.RoomId)
                .ToListAsync();
        }
    }
}