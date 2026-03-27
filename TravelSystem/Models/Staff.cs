using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int? EmployeeCode { get; set; }

    public DateOnly? HireDate { get; set; }

    public string? WorkStatus { get; set; }

    public virtual ICollection<RequestCancel> RequestCancels { get; set; } = new List<RequestCancel>();

    public virtual User StaffNavigation { get; set; } = null!;
}
