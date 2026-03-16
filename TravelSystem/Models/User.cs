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

    public virtual ICollection<BookDetail> BookDetails { get; set; } = new List<BookDetail>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<PendingRecharge> PendingRecharges { get; set; } = new List<PendingRecharge>();

    public virtual ICollection<RequestCancel> RequestCancels { get; set; } = new List<RequestCancel>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<TravelAgent> TravelAgents { get; set; } = new List<TravelAgent>();

    public virtual Wallet? Wallet { get; set; }

    public virtual ICollection<WithdrawRequest> WithdrawRequests { get; set; } = new List<WithdrawRequest>();
}
