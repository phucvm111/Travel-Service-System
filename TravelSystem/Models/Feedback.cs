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

    public int? Status { get; set; }

    public int? BookId { get; set; }

    public int? UserId { get; set; }

    public virtual BookDetail? Book { get; set; }

    public virtual User? User { get; set; }
}
