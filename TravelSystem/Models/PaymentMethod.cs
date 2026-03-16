using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class PaymentMethod
{
    public int PaymentMethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public virtual ICollection<BookDetail> BookDetails { get; set; } = new List<BookDetail>();

    public virtual ICollection<PendingRecharge> PendingRecharges { get; set; } = new List<PendingRecharge>();
}
