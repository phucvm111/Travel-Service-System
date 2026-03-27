using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TravelSystem.Models;

public partial class FinalPrnContext : DbContext
{
    public FinalPrnContext()
    {
    }

    public FinalPrnContext(DbContextOptions<FinalPrnContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Accommodation> Accommodations { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Entertainment> Entertainments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<FundRequest> FundRequests { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<RequestCancel> RequestCancels { get; set; }

    public virtual DbSet<Restaurant> Restaurants { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Tour> Tours { get; set; }

    public virtual DbSet<TourDeparture> TourDepartures { get; set; }

    public virtual DbSet<TourServiceDetail> TourServiceDetails { get; set; }

    public virtual DbSet<Tourist> Tourists { get; set; }

    public virtual DbSet<TransactionHistory> TransactionHistories { get; set; }

    public virtual DbSet<TravelAgent> TravelAgents { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vat> Vats { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-6HNG787;Initial Catalog=FinalPRN8; Trusted_Connection=SSPI;Encrypt=false;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Accommodation>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Accommod__4550733F43CA2083");

            entity.Property(e => e.ServiceId)
                .ValueGeneratedNever()
                .HasColumnName("serviceID");
            entity.Property(e => e.CheckInTime).HasColumnName("checkInTime");
            entity.Property(e => e.CheckOutTime).HasColumnName("checkOutTime");
            entity.Property(e => e.StarRating).HasColumnName("starRating");

            entity.HasOne(d => d.Service).WithOne(p => p.Accommodation)
                .HasForeignKey<Accommodation>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Accommodations_Service");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PK__Booking__8BE5A12DD47D5374");

            entity.ToTable("Booking");

            entity.HasIndex(e => e.BookCode, "UQ__Booking__3BB8DAE6CF8C8F8E").IsUnique();

            entity.Property(e => e.BookId).HasColumnName("bookID");
            entity.Property(e => e.BookCode).HasColumnName("bookCode");
            entity.Property(e => e.BookDate).HasColumnName("bookDate");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("firstName");
            entity.Property(e => e.Gmail)
                .HasMaxLength(255)
                .HasColumnName("gmail");
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
            entity.Property(e => e.TourDepartureId).HasColumnName("tourDepartureID");
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.VoucherId).HasColumnName("voucherID");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PaymentMethodId)
                .HasConstraintName("FK_Booking_PaymentMethod");

            entity.HasOne(d => d.TourDeparture).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.TourDepartureId)
                .HasConstraintName("FK_Booking_TourDeparture");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Booking_User");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_Booking_Voucher");
        });

        modelBuilder.Entity<Entertainment>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Entertai__4550733F27D2BCC5");

            entity.ToTable("Entertainment");

            entity.Property(e => e.ServiceId)
                .ValueGeneratedNever()
                .HasColumnName("serviceID");
            entity.Property(e => e.CloseTime).HasColumnName("closeTime");
            entity.Property(e => e.DayOfWeekOpen)
                .HasMaxLength(100)
                .HasColumnName("dayOfWeekOpen");
            entity.Property(e => e.OpenTime).HasColumnName("openTime");
            entity.Property(e => e.TicketPrice).HasColumnName("ticketPrice");

            entity.HasOne(d => d.Service).WithOne(p => p.Entertainment)
                .HasForeignKey<Entertainment>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Entertainment_Service");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__2613FDC4D49D9306");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("feedbackID");
            entity.Property(e => e.BookId).HasColumnName("bookID");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.Rate).HasColumnName("rate");

            entity.HasOne(d => d.Book).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Booking");
        });

        modelBuilder.Entity<FundRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FundRequ__3214EC27DEE0BDA2");

            entity.ToTable("FundRequest");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ApproveBy).HasColumnName("approveBy");
            entity.Property(e => e.ApprovedDate)
                .HasColumnType("datetime")
                .HasColumnName("approvedDate");
            entity.Property(e => e.BankAccountSnapshot)
                .HasMaxLength(100)
                .HasColumnName("bankAccountSnapshot");
            entity.Property(e => e.BankNameSnapshot)
                .HasMaxLength(100)
                .HasColumnName("bankNameSnapshot");
            entity.Property(e => e.CreateBy).HasColumnName("createBy");
            entity.Property(e => e.ReferenceCode)
                .HasMaxLength(100)
                .HasColumnName("referenceCode");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("requestDate");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");

            entity.HasOne(d => d.ApproveByNavigation).WithMany(p => p.FundRequestApproveByNavigations)
                .HasForeignKey(d => d.ApproveBy)
                .HasConstraintName("FK_FundRequest_ApproveBy");

            entity.HasOne(d => d.CreateByNavigation).WithMany(p => p.FundRequestCreateByNavigations)
                .HasForeignKey(d => d.CreateBy)
                .HasConstraintName("FK_FundRequest_CreateBy");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PK__PaymentM__46612FD86DB86E06");

            entity.ToTable("PaymentMethod");

            entity.Property(e => e.PaymentMethodId).HasColumnName("paymentMethodID");
            entity.Property(e => e.MethodName)
                .HasMaxLength(100)
                .HasColumnName("methodName");
        });

        modelBuilder.Entity<RequestCancel>(entity =>
        {
            entity.HasKey(e => e.RequestCancelId).HasName("PK__RequestC__ED69358C086CC394");

            entity.ToTable("RequestCancel");

            entity.Property(e => e.RequestCancelId).HasColumnName("requestCancelID");
            entity.Property(e => e.BookId).HasColumnName("bookID");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequestDate).HasColumnName("requestDate");
            entity.Property(e => e.StaffId).HasColumnName("staffID");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Book).WithMany(p => p.RequestCancels)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK_RequestCancel_Booking");

            entity.HasOne(d => d.Staff).WithMany(p => p.RequestCancels)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_RequestCancel_Staff");
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Restaura__4550733F4F54D490");

            entity.ToTable("Restaurant");

            entity.Property(e => e.ServiceId)
                .ValueGeneratedNever()
                .HasColumnName("serviceID");
            entity.Property(e => e.CloseTime).HasColumnName("closeTime");
            entity.Property(e => e.OpenTime).HasColumnName("openTime");
            entity.Property(e => e.RestaurantType)
                .HasMaxLength(100)
                .HasColumnName("restaurantType");

            entity.HasOne(d => d.Service).WithOne(p => p.Restaurant)
                .HasForeignKey<Restaurant>(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Restaurant_Service");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__CD98460AD627E99C");

            entity.ToTable("Role");

            entity.Property(e => e.RoleId).HasColumnName("roleID");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .HasColumnName("roleName");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Service__4550733F2EC8F0DF");

            entity.ToTable("Service");

            entity.Property(e => e.ServiceId).HasColumnName("serviceID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.AgentId).HasColumnName("agentID");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .HasColumnName("serviceName");
            entity.Property(e => e.ServiceType).HasColumnName("serviceType");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");

            entity.HasOne(d => d.Agent).WithMany(p => p.Services)
                .HasForeignKey(d => d.AgentId)
                .HasConstraintName("FK_Service_TravelAgent");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__6465E19E8E52B11C");

            entity.Property(e => e.StaffId)
                .ValueGeneratedNever()
                .HasColumnName("staffID");
            entity.Property(e => e.EmployeeCode).HasColumnName("employeeCode");
            entity.Property(e => e.HireDate).HasColumnName("hireDate");
            entity.Property(e => e.WorkStatus)
                .HasMaxLength(50)
                .HasColumnName("workStatus");

            entity.HasOne(d => d.StaffNavigation).WithOne(p => p.Staff)
                .HasForeignKey<Staff>(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Staff_User");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.TourId).HasName("PK__Tour__519D1D03AD8C2E95");

            entity.ToTable("Tour");

            entity.Property(e => e.TourId).HasColumnName("tourID");
            entity.Property(e => e.EndPlace)
                .HasMaxLength(255)
                .HasColumnName("endPlace");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.NumberOfDay).HasColumnName("numberOfDay");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.StartPlace)
                .HasMaxLength(255)
                .HasColumnName("startPlace");
            entity.Property(e => e.Status).HasColumnName("status");
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tour_TravelAgent");
        });

        modelBuilder.Entity<TourDeparture>(entity =>
        {
            entity.HasKey(e => e.DepartureId).HasName("PK__TourDepa__2BFAAAF529354A4F");

            entity.ToTable("TourDeparture");

            entity.Property(e => e.DepartureId).HasColumnName("departureID");
            entity.Property(e => e.AdultPrice).HasColumnName("adultPrice");
            entity.Property(e => e.AvailableSeat).HasColumnName("availableSeat");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.ChildPrice).HasColumnName("childPrice");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.StartDate).HasColumnName("startDate");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("status");
            entity.Property(e => e.TourId).HasColumnName("tourID");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourDepartures)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK_TourDeparture_Tour");
        });

        modelBuilder.Entity<TourServiceDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__Tour_Ser__830778396C36C2B5");

            entity.ToTable("Tour_Service_Detail");

            entity.Property(e => e.DetailId).HasColumnName("detailID");
            entity.Property(e => e.ServiceId).HasColumnName("serviceID");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .HasColumnName("serviceName");
            entity.Property(e => e.TourId).HasColumnName("tourID");

            entity.HasOne(d => d.Service).WithMany(p => p.TourServiceDetails)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_TourServiceDetail_Service");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourServiceDetails)
                .HasForeignKey(d => d.TourId)
                .HasConstraintName("FK_TourServiceDetail_Tour");
        });

        modelBuilder.Entity<Tourist>(entity =>
        {
            entity.HasKey(e => e.TouristId).HasName("PK__Tourist__BBB2E8D7EEC0358B");

            entity.ToTable("Tourist");

            entity.Property(e => e.TouristId)
                .ValueGeneratedNever()
                .HasColumnName("touristID");
            entity.Property(e => e.AccountHolderName)
                .HasMaxLength(100)
                .HasColumnName("accountHolderName");
            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .HasColumnName("bankName");
            entity.Property(e => e.BankNumber)
                .HasMaxLength(50)
                .HasColumnName("bankNumber");
            entity.Property(e => e.IdCard)
                .HasMaxLength(100)
                .HasColumnName("idCard");
            entity.Property(e => e.IdCardBackImage)
                .HasMaxLength(255)
                .HasColumnName("idCardBackImage");
            entity.Property(e => e.IdCardFrontImage)
                .HasMaxLength(255)
                .HasColumnName("idCardFrontImage");

            entity.HasOne(d => d.TouristNavigation).WithOne(p => p.Tourist)
                .HasForeignKey<Tourist>(d => d.TouristId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tourist_User");
        });

        modelBuilder.Entity<TransactionHistory>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__9B57CF52CFB7188C");

            entity.ToTable("TransactionHistory");

            entity.Property(e => e.TransactionId).HasColumnName("transactionID");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
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
                .HasConstraintName("FK_TransactionHistory_User");
        });

        modelBuilder.Entity<TravelAgent>(entity =>
        {
            entity.HasKey(e => e.TravelAgentId).HasName("PK__TravelAg__8F86E844FB595FC4");

            entity.ToTable("TravelAgent");

            entity.Property(e => e.TravelAgentId).HasColumnName("travelAgentID");
            entity.Property(e => e.BackIdcard)
                .HasMaxLength(100)
                .HasColumnName("backIDCard");
            entity.Property(e => e.BusinessLicense)
                .HasMaxLength(100)
                .HasColumnName("businessLicense");
            entity.Property(e => e.DateOfIssue).HasColumnName("dateOfIssue");
            entity.Property(e => e.EstablishMentDate).HasColumnName("establishMentDate");
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
            entity.Property(e => e.TravelAgentEmail)
                .HasMaxLength(255)
                .HasColumnName("travelAgentEmail");
            entity.Property(e => e.TravelAgentName)
                .HasMaxLength(100)
                .HasColumnName("travelAgentName");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.User).WithMany(p => p.TravelAgents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TravelAgent_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF33ED10AF");

            entity.ToTable("User");

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
                .HasMaxLength(20)
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
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<Vat>(entity =>
        {
            entity.HasKey(e => e.VatId).HasName("PK__VAT__429329E09EF64171");

            entity.ToTable("VAT");

            entity.Property(e => e.VatId).HasColumnName("vatID");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.StartDate).HasColumnName("startDate");
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasColumnName("status");
            entity.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("updateDate");
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.VatRate).HasColumnName("vatRate");

            entity.HasOne(d => d.User).WithMany(p => p.Vats)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_VAT_User");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Voucher__F5338989FB2914D7");

            entity.ToTable("Voucher");

            entity.HasIndex(e => e.VoucherCode, "UQ__Voucher__09FEFFB08D5DDC98").IsUnique();

            entity.Property(e => e.VoucherId).HasColumnName("voucherID");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.MaxDiscountAmount).HasColumnName("maxDiscountAmount");
            entity.Property(e => e.MinDiscountAmount).HasColumnName("minDiscountAmount");
            entity.Property(e => e.PercentDiscount).HasColumnName("percentDiscount");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.StartDate).HasColumnName("startDate");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(100)
                .HasColumnName("voucherCode");
            entity.Property(e => e.VoucherName)
                .HasMaxLength(255)
                .HasColumnName("voucherName");

            entity.HasOne(d => d.User).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Voucher_User");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Wallet__CB9A1CDFF694BFBB");

            entity.ToTable("Wallet");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.Balance)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wallet_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
