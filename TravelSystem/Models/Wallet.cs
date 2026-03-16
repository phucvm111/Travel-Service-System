using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Wallet
{
    public int UserId { get; set; }

    public decimal? Balance { get; set; }

    public DateTime? CreateDate { get; set; }

    public virtual ICollection<TransactionHistory> TransactionHistories { get; set; } = new List<TransactionHistory>();

    public virtual User User { get; set; } = null!;
}
