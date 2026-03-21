using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Vouchers
{
    public class DetailsModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public DetailsModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public Voucher Voucher { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
            {
                TempData["Error"] = "Không tìm thấy voucher.";
                return RedirectToPage("/Agents/Vouchers/Index");
            }

            Voucher = voucher;
            return Page();
        }
    }
}