using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Tour
{
    public int TourId { get; set; }

    public int? TravelAgentId { get; set; }

    public string? TourName { get; set; }

    public int? NumberOfDay { get; set; }

    public string? StartPlace { get; set; }

    public string? EndPlace { get; set; }

    public int? Quantity { get; set; }

    public string? Image { get; set; }

    public DateOnly? StartDay { get; set; }

    public DateOnly? EndDay { get; set; }

    public double? AdultPrice { get; set; }

    public double? ChildrenPrice { get; set; }

    public string? TourIntroduce { get; set; }

    public string? TourSchedule { get; set; }

    public string? TourInclude { get; set; }

    public string? TourNonInclude { get; set; }

    public double? Rate { get; set; }

    public int? Status { get; set; }

    public virtual ICollection<BookDetail> BookDetails { get; set; } = new List<BookDetail>();

    public virtual ICollection<TourServiceDetail> TourServiceDetails { get; set; } = new List<TourServiceDetail>();

    public virtual TravelAgent? TravelAgent { get; set; }
}
