using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class BookDetail
{
    public int BookId { get; set; }

    public int? UserId { get; set; }

    public int? TourId { get; set; }

    public int? VoucherId { get; set; }

    public DateOnly? BookDate { get; set; }

    public int? NumberAdult { get; set; }

    public int? NumberChildren { get; set; }

    public int? PaymentMethodId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Phone { get; set; }

    public string? Gmail { get; set; }

    public string? Note { get; set; }

    public int? IsBookedForOther { get; set; }

    public double? TotalPrice { get; set; }

    public int? Status { get; set; }

    public long? BookCode { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual ICollection<RequestCancel> RequestCancels { get; set; } = new List<RequestCancel>();

    public virtual Tour? Tour { get; set; }

    public virtual User? User { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
