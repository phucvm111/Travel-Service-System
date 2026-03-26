using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TravelSystem.Models;

public partial class Accommodation
{
    public int ServiceId { get; set; }
    public TimeOnly? CheckInTime { get; set; }
    public int? StarRating { get; set; }
    public TimeOnly? CheckOutTime { get; set; }

    [ValidateNever]
    public virtual Service Service { get; set; } = null!;
}