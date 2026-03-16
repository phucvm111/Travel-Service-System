using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int? AccommodationId { get; set; }

    public string? RoomTypes { get; set; }

    public int? NumberOfRooms { get; set; }

    public double? PriceOfRoom { get; set; }

    public virtual Accommodation? Accommodation { get; set; }
}
