using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public double? Rate { get; set; }

    public string? Image { get; set; }

    public string? Content { get; set; }

    public DateTime? CreateDate { get; set; }

    public int BookId { get; set; }

    public virtual Booking Book { get; set; } = null!;
}
