using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Accommodation
{
    public int ServiceId { get; set; }

    public TimeOnly? CheckInTime { get; set; }

    public int? StarRating { get; set; }

    public TimeOnly? CheckOutTime { get; set; }

    public virtual Service Service { get; set; } = null!;
}
