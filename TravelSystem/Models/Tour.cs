using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Tour
{
    public int TourId { get; set; }

    public int TravelAgentId { get; set; }

    public string TourName { get; set; } = null!;

    public int NumberOfDay { get; set; }

    public string StartPlace { get; set; } = null!;

    public string EndPlace { get; set; } = null!;

    public string Image { get; set; } = null!;

    public string? TourIntroduce { get; set; }

    public string? TourSchedule { get; set; }

    public string? TourInclude { get; set; }

    public string? TourNonInclude { get; set; }

    public double? Rate { get; set; }

    public int? Status { get; set; }

    public virtual ICollection<TourDeparture> TourDepartures { get; set; } = new List<TourDeparture>();

    public virtual ICollection<TourServiceDetail> TourServiceDetails { get; set; } = new List<TourServiceDetail>();

    public virtual TravelAgent TravelAgent { get; set; } = null!;
}
