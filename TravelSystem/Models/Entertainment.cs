using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Entertainment
{
    public int EntertainmentId { get; set; }

    public int ServiceId { get; set; }

    public string? Name { get; set; }

    public string? Image { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Description { get; set; }

    public TimeOnly? TimeOpen { get; set; }

    public TimeOnly? TimeClose { get; set; }

    public string? DayOfWeekOpen { get; set; }

    public double? TicketPrice { get; set; }

    public double? Rate { get; set; }  

    public string? Status { get; set; }

    public string? EntertainmentType { get; set; }


    public virtual Service? Service { get; set; }
}
