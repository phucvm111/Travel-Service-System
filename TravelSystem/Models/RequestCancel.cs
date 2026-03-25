using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class RequestCancel
{
    public int RequestCancelId { get; set; }

    public int? BookId { get; set; }

    public int? StaffId { get; set; }

    public DateOnly? RequestDate { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public virtual Booking? Book { get; set; }

    public virtual Staff? Staff { get; set; }
}
