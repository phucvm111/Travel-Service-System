using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class TravelAgent
{
    public int TravelAgentId { get; set; }

    public int UserId { get; set; }

    public string? TravelAgentName { get; set; }

    public string? TravelAgentAddress { get; set; }

    public string? TravelAgentEmail { get; set; }

    public string? HotLine { get; set; }

    public string? TaxCode { get; set; }

    public string? BusinessLicense { get; set; }

    public DateOnly? EstablishMentDate { get; set; }

    public string? FrontIdcard { get; set; }

    public string? BackIdcard { get; set; }

    public string? RepresentativeIdcard { get; set; }

    public DateOnly? DateOfIssue { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>();

    public virtual User User { get; set; } = null!;
}
