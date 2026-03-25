using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public int? AgentId { get; set; }

    public int? ServiceType { get; set; }

    public string? ServiceName { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }

    public int? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Accommodation? Accommodation { get; set; }

    public virtual TravelAgent? Agent { get; set; }

    public virtual Entertainment? Entertainment { get; set; }

    public virtual Restaurant? Restaurant { get; set; }

    public virtual ICollection<TourServiceDetail> TourServiceDetails { get; set; } = new List<TourServiceDetail>();
}
