using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Vat
{
    public int VatId { get; set; }

    public double? VatRate { get; set; }

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
