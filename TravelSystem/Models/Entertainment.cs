using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Entertainment
{
    public int ServiceId { get; set; }

    public TimeOnly? OpenTime { get; set; }

    public TimeOnly? CloseTime { get; set; }

    public string? DayOfWeekOpen { get; set; }

    public double? TicketPrice { get; set; }

    public virtual Service Service { get; set; } = null!;
}
