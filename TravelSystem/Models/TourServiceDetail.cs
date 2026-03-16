using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class TourServiceDetail
{
    public int DetailId { get; set; }

    public int? TourId { get; set; }

    public int? ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public virtual Service? Service { get; set; }

    public virtual Tour? Tour { get; set; }
}
