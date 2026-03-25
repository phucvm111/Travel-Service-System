using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class PaymentMethod
{
    public int PaymentMethodId { get; set; }

    public string? MethodName { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
