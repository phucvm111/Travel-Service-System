using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class FundRequest
{
    public int Id { get; set; }

    public int? CreateBy { get; set; }

    public int? ApproveBy { get; set; }

    public decimal? Amount { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string? ReferenceCode { get; set; }

    public string? BankAccountSnapshot { get; set; }

    public string? BankNameSnapshot { get; set; }

    public string? Status { get; set; }

    public string? Type { get; set; }

    public virtual User? ApproveByNavigation { get; set; }

    public virtual User? CreateByNavigation { get; set; }
}
