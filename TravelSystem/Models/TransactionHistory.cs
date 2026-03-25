using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class TransactionHistory
{
    public int TransactionId { get; set; }

    public int? UserId { get; set; }

    public decimal? Amount { get; set; }

    public string? TransactionType { get; set; }

    public string? Description { get; set; }

    public DateTime? TransactionDate { get; set; }

    public virtual User? User { get; set; }
}
