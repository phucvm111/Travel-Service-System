using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Vouchers
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public IndexModel(FinalPrnContext context)
        {
            _context = context;
        }

        public IList<Voucher> VoucherList { get; set; } = new List<Voucher>();

        [BindProperty(SupportsGet = true)]
        public string? SearchCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterStatus { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<Voucher> query = _context.Vouchers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchCode))
            {
                query = query.Where(v =>
                    v.VoucherCode != null &&
                    v.VoucherCode.Contains(SearchCode));
            }

            if (FilterStatus.HasValue)
            {
                query = query.Where(v => v.Status == FilterStatus.Value);
            }

            VoucherList = await query
                .OrderByDescending(v => v.VoucherId)
                .ToListAsync();
        }
    }
}