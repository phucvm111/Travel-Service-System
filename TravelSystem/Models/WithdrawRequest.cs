using System;
using System.Collections.Generic;

namespace TravelSystem.Models;

public partial class WithdrawRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Amount { get; set; }

    public string Email { get; set; } = null!;

    public string BankName { get; set; } = null!;

    public string BankAccountNumber { get; set; } = null!;

    public string AccountHolderName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string IdCard { get; set; } = null!;

    public string? IdCardFrontImage { get; set; }

    public string? IdCardBackImage { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
