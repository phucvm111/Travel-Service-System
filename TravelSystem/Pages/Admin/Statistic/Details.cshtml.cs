using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Statistic
{
    public class DetailsModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private const int CompletedStatus = 3;

        public DetailsModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public BookDetail BookDetail { get; set; } = default!;
        public Tour? Tour { get; set; }
        public User? UserInfo { get; set; }
        public Voucher? Voucher { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public double TaxAmount { get; set; }
        public double FinalRevenue { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
                return RedirectToPage("/Auths/Login");

            BookDetail = await _context.BookDetails
                .Include(b => b.Tour)
                .Include(b => b.User)
                .Include(b => b.Voucher)
                .Include(b => b.PaymentMethod)
                .FirstOrDefaultAsync(b => b.BookId == id && b.Status == CompletedStatus);

            if (BookDetail == null)
                return NotFound();

            Tour = BookDetail.Tour;
            UserInfo = BookDetail.User;
            Voucher = BookDetail.Voucher;
            PaymentMethod = BookDetail.PaymentMethod;

            var activeVat = await _context.Vats
                .Where(v => v.Status == 1)
                .OrderByDescending(v => v.StartDate)
                .FirstOrDefaultAsync();

            double vatRate = activeVat?.VatRate ?? 0;
            TaxAmount = (BookDetail.TotalPrice ?? 0) * vatRate / 100.0;
            FinalRevenue = (BookDetail.TotalPrice ?? 0) - TaxAmount;

            return Page();
        }
        }
}
