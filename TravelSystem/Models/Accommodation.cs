using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Accommodation
{
    public int AccommodationId { get; set; }

    public int ServiceId { get; set; }

    public string? Name { get; set; }

    public string? Image { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Description { get; set; }

    public TimeOnly? CheckInTime { get; set; }

    public TimeOnly? CheckOutTime { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual Service? Service { get; set; }
}
