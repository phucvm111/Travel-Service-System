using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Wallet
{
    public int UserId { get; set; }

    public decimal? Balance { get; set; }

    public virtual User User { get; set; } = null!;
}
