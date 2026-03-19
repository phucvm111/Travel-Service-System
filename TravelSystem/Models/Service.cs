using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public string? ServiceType { get; set; }

    public int? TravelAgentId { get; set; }

    public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();

    public virtual ICollection<Entertainment> Entertainments { get; set; } = new List<Entertainment>();

    public virtual ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();

    public virtual ICollection<TourServiceDetail> TourServiceDetails { get; set; } = new List<TourServiceDetail>();

    public virtual TravelAgent? TravelAgent { get; set; }
    //public int? UserID { get; internal set; }
}
