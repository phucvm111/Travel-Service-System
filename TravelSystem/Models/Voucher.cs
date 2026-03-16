using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string? VoucherCode { get; set; }

    public string? VoucherName { get; set; }

    public string? Description { get; set; }

    public double? PercentDiscount { get; set; }

    public double? MaxDiscountAmount { get; set; }

    public double? MinAmountApply { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? Quantity { get; set; }

    public int? Status { get; set; }

    public virtual ICollection<BookDetail> BookDetails { get; set; } = new List<BookDetail>();
}
