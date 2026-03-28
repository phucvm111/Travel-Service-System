using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? Gmail { get; set; }

    public string? Password { get; set; }

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public DateOnly? CreateAt { get; set; }

    public DateOnly? UpdateAt { get; set; }

    public int? Status { get; set; }

    public int RoleId { get; set; }

    public decimal? Balance { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<FundRequest> FundRequestApproveByNavigations { get; set; } = new List<FundRequest>();

    public virtual ICollection<FundRequest> FundRequestCreateByNavigations { get; set; } = new List<FundRequest>();

    public virtual Role? Role { get; set; }

    public virtual Staff? Staff { get; set; }

    public virtual Tourist? Tourist { get; set; }

    public virtual ICollection<TransactionHistory> TransactionHistories { get; set; } = new List<TransactionHistory>();

    public virtual ICollection<TravelAgent> TravelAgents { get; set; } = new List<TravelAgent>();

    public virtual ICollection<Vat> Vats { get; set; } = new List<Vat>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
