using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TravelSystem.Models;

public partial class Prn222PrjContext : DbContext
{
    public Prn222PrjContext()
    {
    }

    public Prn222PrjContext(DbContextOptions<Prn222PrjContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Accommodation> Accommodations { get; set; }

    public virtual DbSet<BookDetail> BookDetails { get; set; }

    public virtual DbSet<Entertainment> Entertainments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PendingRecharge> PendingRecharges { get; set; }

    public virtual DbSet<RequestCancel> RequestCancels { get; set; }

    public virtual DbSet<Restaurant> Restaurants { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Tour> Tours { get; set; }

    public virtual DbSet<TourServiceDetail> TourServiceDetails { get; set; }

    public virtual DbSet<TransactionHistory> TransactionHistories { get; set; }

    public virtual DbSet<TravelAgent> TravelAgents { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vat> Vats { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WithdrawRequest> WithdrawRequests { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
       // => optionsBuilder.UseSqlServer("Data Source=DESKTOP-AQ0BKFI\\SQLEXPRESS;Initial Catalog=PRN222_Prj; Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Accommodation>(entity =>
        {
            entity.HasKey(e => e.AccommodationId).HasName("PK__Accommod__20C0A5FDB071B2C9");

            entity.ToTable("Accommodation");

            entity.Property(e => e.AccommodationId).HasColumnName("accommodationId");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.CheckInTime).HasColumnName("checkInTime");
            entity.Property(e => e.CheckOutTime).HasColumnName("checkOutTime");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.ServiceId).HasColumnName("serviceId");

            entity.HasOne(d => d.Service).WithMany(p => p.Accommodations)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Accommoda__servi__49C3F6B7");
        });

        modelBuilder.Entity<BookDetail>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PK__BookDeta__8BE5A12DA6D55BAC");

            entity.ToTable("BookDetail");

            entity.HasIndex(e => e.BookCode, "UQ__BookDeta__3BB8DAE64B4A7B93").IsUnique();

            entity.Property(e => e.BookId).HasColumnName("bookID");
            entity.Property(e => e.BookCode).HasColumnName("bookCode");
            entity.Property(e => e.BookDate).HasColumnName("bookDate");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("firstName");
            entity.Property(e => e.Gmail)
                .HasMaxLength(255)
                .HasColumnName("gmail");
            entity.Property(e => e.IsBookedForOther).HasColumnName("isBookedForOther");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("lastName");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.NumberAdult).HasColumnName("numberAdult");
            entity.Property(e => e.NumberChildren).HasColumnName("numberChildren");
            entity.Property(e => e.PaymentMethodId).HasColumnName("paymentMethodID");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalPrice).HasColumnName("totalPrice");
            entity.Property(e => e.TourId).HasColumnName("tourID");
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.VoucherId).HasColumnName("voucherID");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.BookDetails)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK__BookDetai__bookC__5EBF139D");

            entity.HasOne(d => d.Tour).WithMany(p => p.BookDetails)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK__BookDetai__tourI__5CD6CB2B");

            entity.HasOne(d => d.User).WithMany(p => p.BookDetails)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__BookDetai__userI__5BE2A6F2");

            entity.HasOne(d => d.Voucher).WithMany(p => p.BookDetails)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK__BookDetai__vouch__5DCAEF64");
        });

        modelBuilder.Entity<Entertainment>(entity =>
        {
            entity.HasKey(e => e.EntertainmentId).HasName("PK__Entertai__E9EE723F653E8843");

            entity.ToTable("Entertainment");

            entity.Property(e => e.EntertainmentId).HasColumnName("entertainmentId");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.DayOfWeekOpen)
                .HasMaxLength(100)
                .HasColumnName("dayOfWeekOpen");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.ServiceId).HasColumnName("serviceId");
            entity.Property(e => e.TicketPrice).HasColumnName("ticketPrice");
            entity.Property(e => e.TimeClose).HasColumnName("timeClose");
            entity.Property(e => e.TimeOpen).HasColumnName("timeOpen");

            entity.HasOne(d => d.Service).WithMany(p => p.Entertainments)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Entertain__servi__52593CB8");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__2613FDC4189FED7A");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("feedbackID");
            entity.Property(e => e.BookId).HasColumnName("bookID");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.Book).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__Feedback__bookID__693CA210");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Feedback__userID__6A30C649");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__PaymentM__46612FD87ED41A6A");

            entity.ToTable("PaymentMethod");

            entity.Property(e => e.PaymentMethodId).HasColumnName("paymentMethodID");
            entity.Property(e => e.MethodName)
                .HasMaxLength(100)
                .HasColumnName("methodName");
        });

        modelBuilder.Entity<PendingRecharge>(entity =>
        {
            entity.HasKey(e => e.RechargeId).HasName("PK__PendingR__04E7F6ACB786719B");

            entity.ToTable("PendingRecharge");

            entity.HasIndex(e => e.ReferenceCode, "UQ__PendingR__024E23F16B4CF835").IsUnique();

            entity.Property(e => e.RechargeId).HasColumnName("rechargeID");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ApproveDate)
                .HasColumnType("datetime")
                .HasColumnName("approveDate");
            entity.Property(e => e.PaymentMethodId).HasColumnName("paymentMethodID");
            entity.Property(e => e.ReferenceCode)
                .HasMaxLength(50)
                .HasColumnName("referenceCode");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("requestDate");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.PendingRecharges)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK__PendingRe__payme__7B5B524B");

            entity.HasOne(d => d.User).WithMany(p => p.PendingRecharges)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PendingRe__userI__7C4F7684");
        });

        modelBuilder.Entity<RequestCancel>(entity =>
        {
            entity.HasKey(e => e.RequestCancelId).HasName("PK__Request___ED69358CE0065122");

            entity.ToTable("Request_Cancel");

            entity.Property(e => e.RequestCancelId).HasColumnName("requestCancelID");
            entity.Property(e => e.BookId).HasColumnName("bookID");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("requestDate");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.Book).WithMany(p => p.RequestCancels)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Request_C__bookI__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.RequestCancels)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Request_C__userI__628FA481");
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(e => e.RestaurantId).HasName("PK__Restaura__09D80A30441D6DDA");

            entity.ToTable("Restaurant");

            entity.Property(e => e.RestaurantId).HasColumnName("restaurantId");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.ServiceId).HasColumnName("serviceId");
            entity.Property(e => e.TimeClose).HasColumnName("timeClose");
            entity.Property(e => e.TimeOpen).HasColumnName("timeOpen");

            entity.HasOne(d => d.Service).WithMany(p => p.Restaurants)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Restauran__servi__4F7CD00D");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__CD98460A397300A7");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId).HasColumnName("roleID");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .HasColumnName("roleName");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__6C3BF5DEBCBEBD11");

            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasColumnName("roomID");
            entity.Property(e => e.AccommodationId).HasColumnName("accommodationID");
            entity.Property(e => e.NumberOfRooms).HasColumnName("numberOfRooms");
            entity.Property(e => e.PriceOfRoom).HasColumnName("priceOfRoom");
            entity.Property(e => e.RoomTypes)
                .HasMaxLength(50)
                .HasColumnName("roomTypes");

            entity.HasOne(d => d.Accommodation).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.AccommodationId)
                .HasConstraintName("FK__Room__accommodat__4CA06362");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Service__455070DFC439E42C");

            entity.ToTable("Service");

            entity.Property(e => e.ServiceId).HasColumnName("serviceId");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .HasColumnName("serviceName");
            entity.Property(e => e.ServiceType)
                .HasMaxLength(255)
                .HasColumnName("serviceType");
            entity.Property(e => e.TravelAgentId).HasColumnName("travelAgentID");

            entity.HasOne(d => d.TravelAgent).WithMany(p => p.Services)
                .HasForeignKey(d => d.TravelAgentId)
                .HasConstraintName("FK__Service__travelA__4316F928");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.TourId).HasName("PK__Tour__519D1D03F99BE582");

            entity.ToTable("Tour");

            entity.Property(e => e.TourId).HasColumnName("tourID");
            entity.Property(e => e.AdultPrice).HasColumnName("adultPrice");
            entity.Property(e => e.ChildrenPrice).HasColumnName("childrenPrice");
            entity.Property(e => e.EndDay).HasColumnName("endDay");
            entity.Property(e => e.EndPlace)
                .HasMaxLength(255)
                .HasColumnName("endPlace");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.NumberOfDay).HasColumnName("numberOfDay");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.StartDay).HasColumnName("startDay");
            entity.Property(e => e.StartPlace)
                .HasMaxLength(255)
                .HasColumnName("startPlace");
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasColumnName("status");
            entity.Property(e => e.TourInclude).HasColumnName("tourInclude");
            entity.Property(e => e.TourIntroduce).HasColumnName("tourIntroduce");
            entity.Property(e => e.TourName)
                .HasMaxLength(255)
                .HasColumnName("tourName");
            entity.Property(e => e.TourNonInclude).HasColumnName("tourNonInclude");
            entity.Property(e => e.TourSchedule).HasColumnName("tourSchedule");
            entity.Property(e => e.TravelAgentId).HasColumnName("travelAgentID");

            entity.HasOne(d => d.TravelAgent).WithMany(p => p.Tours)
                .HasForeignKey(d => d.TravelAgentId)
                .HasConstraintName("FK__Tour__travelAgen__3F466844");
        });

        modelBuilder.Entity<TourServiceDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__Tour_Ser__83077839ABE646DC");

            entity.ToTable("Tour_Service_Detail");

            entity.Property(e => e.DetailId).HasColumnName("detailID");
            entity.Property(e => e.ServiceId).HasColumnName("serviceId");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .HasColumnName("serviceName");
            entity.Property(e => e.TourId).HasColumnName("tourID");

            entity.HasOne(d => d.Service).WithMany(p => p.TourServiceDetails)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__Tour_Serv__servi__46E78A0C");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourServiceDetails)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK__Tour_Serv__tourI__45F365D3");
        });

        modelBuilder.Entity<TransactionHistory>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__9B57CF527C076C10");

            entity.ToTable("TransactionHistory");

            entity.Property(e => e.TransactionId).HasColumnName("transactionID");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.AmountAfterTransaction)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amountAfterTransaction");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("transactionDate");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .HasColumnName("transactionType");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.User).WithMany(p => p.TransactionHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transacti__userI__74AE54BC");
        });

        modelBuilder.Entity<TravelAgent>(entity =>
        {
            entity.HasKey(e => e.TravelAgentId).HasName("PK__TravelAg__8F86E844AF45ED9D");

            entity.ToTable("TravelAgent");

            entity.Property(e => e.TravelAgentId).HasColumnName("travelAgentID");
            entity.Property(e => e.BackIdcard)
                .HasMaxLength(100)
                .HasColumnName("backIDCard");
            entity.Property(e => e.BusinessLicense)
                .HasMaxLength(100)
                .HasColumnName("businessLicense");
            entity.Property(e => e.DateOfIssue).HasColumnName("dateOfIssue");
            entity.Property(e => e.EstablishmentDate).HasColumnName("establishmentDate");
            entity.Property(e => e.FrontIdcard)
                .HasMaxLength(100)
                .HasColumnName("frontIDCard");
            entity.Property(e => e.HotLine)
                .HasMaxLength(20)
                .HasColumnName("hotLine");
            entity.Property(e => e.RepresentativeIdcard)
                .HasMaxLength(50)
                .HasColumnName("representativeIDCard");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasColumnName("taxCode");
            entity.Property(e => e.TravelAgentAddress)
                .HasMaxLength(255)
                .HasColumnName("travelAgentAddress");
            entity.Property(e => e.TravelAgentGmail)
                .HasMaxLength(255)
                .HasColumnName("travelAgentGmail");
            entity.Property(e => e.TravelAgentName)
                .HasMaxLength(100)
                .HasColumnName("travelAgentName");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.User).WithMany(p => p.TravelAgents)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__TravelAge__userI__3C69FB99");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF9C535816");

            entity.ToTable("User", tb => tb.HasTrigger("trg_CreateWallet_AfterInsert_User"));

            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.CreateAt).HasColumnName("create_at");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("firstName");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.Gmail)
                .HasMaxLength(255)
                .HasColumnName("gmail");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("lastName");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("roleID");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdateAt).HasColumnName("update_at");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__User__roleID__398D8EEE");
        });

        modelBuilder.Entity<Vat>(entity =>
        {
            entity.HasKey(e => e.VatId).HasName("PK__VAT__429329E0D451AE3B");

            entity.ToTable("VAT");

            entity.Property(e => e.VatId).HasColumnName("vatID");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.StartDate).HasColumnName("startDate");
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasColumnName("status");
            entity.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("updateDate");
            entity.Property(e => e.VatRate).HasColumnName("vatRate");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Voucher__F5338989697EEFEC");

            entity.ToTable("Voucher");

            entity.HasIndex(e => e.VoucherCode, "UQ__Voucher__09FEFFB09AE7231F").IsUnique();

            entity.Property(e => e.VoucherId).HasColumnName("voucherID");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.MaxDiscountAmount).HasColumnName("maxDiscountAmount");
            entity.Property(e => e.MinAmountApply).HasColumnName("minAmountApply");
            entity.Property(e => e.PercentDiscount).HasColumnName("percentDiscount");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.StartDate).HasColumnName("startDate");
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasColumnName("status");
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(100)
                .HasColumnName("voucherCode");
            entity.Property(e => e.VoucherName)
                .HasMaxLength(255)
                .HasColumnName("voucherName");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Wallet__CB9A1CDF58AF73E8");

            entity.ToTable("Wallet");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.Balance)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createDate");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wallet__userID__6FE99F9F");
        });

        modelBuilder.Entity<WithdrawRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Withdraw__3213E83FA94612D3");

            entity.ToTable("WithdrawRequest");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountHolderName)
                .HasMaxLength(100)
                .HasColumnName("accountHolderName");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.ApprovedAt)
                .HasColumnType("datetime")
                .HasColumnName("approvedAt");
            entity.Property(e => e.BankAccountNumber)
                .HasMaxLength(50)
                .HasColumnName("bankAccountNumber");
            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .HasColumnName("bankName");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.IdCard)
                .HasMaxLength(255)
                .HasColumnName("idCard");
            entity.Property(e => e.IdCardBackImage)
                .HasMaxLength(255)
                .HasColumnName("idCardBackImage");
            entity.Property(e => e.IdCardFrontImage)
                .HasMaxLength(255)
                .HasColumnName("idCardFrontImage");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.WithdrawRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Withdraw_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
