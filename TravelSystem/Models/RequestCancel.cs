using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class RequestCancel
{
    public int RequestCancelId { get; set; }

    public int BookId { get; set; }

    public int UserId { get; set; }

    public DateOnly? RequestDate { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public virtual BookDetail Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
