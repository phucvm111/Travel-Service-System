using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class PendingRecharge
{
    public int RechargeId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string ReferenceCode { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? RequestDate { get; set; }

    public DateTime? ApproveDate { get; set; }

    public int? PaymentMethodId { get; set; }

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual User User { get; set; } = null!;
}
