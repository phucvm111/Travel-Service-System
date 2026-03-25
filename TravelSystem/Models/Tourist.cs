using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Tourist
{
    public int TouristId { get; set; }

    public string? AccountHolderName { get; set; }

    public string? IdCard { get; set; }

    public string? IdCardFrontImage { get; set; }

    public string? IdCardBackImage { get; set; }

    public string? BankName { get; set; }

    public string? BankNumber { get; set; }

    public virtual User TouristNavigation { get; set; } = null!;
}
