using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;

namespace TravelSystem.Pages.Wallet
{
    public class DepositPaymentFalseModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DepositPaymentFalseModel(FinalPrnContext context)
        {
            _context = context;
        }

        public string PaymentCode { get; set; }

        public void OnGet(string referenceCode)
        {
            PaymentCode = referenceCode;

            var recharge = _context.FundRequests.FirstOrDefault(r => r.ReferenceCode == referenceCode);
            if (recharge != null)
            {
                recharge.Status = "CANCEL";
                _context.SaveChanges();
            }
            
        }
    }
}