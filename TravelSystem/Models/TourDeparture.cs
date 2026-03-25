using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class TourDeparture
{
    public int DepartureId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? TourId { get; set; }

    public int? Capacity { get; set; }

    public int? AvailableSeat { get; set; }

    public double? AdultPrice { get; set; }

    public double? ChildPrice { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Tour? Tour { get; set; }
}
