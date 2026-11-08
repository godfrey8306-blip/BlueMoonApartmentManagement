using Microsoft.EntityFrameworkCore;
using BlueMoon.API.Models;

namespace BlueMoon.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<Household> Households { get; set; }
    public DbSet<Resident> Residents { get; set; }
    public DbSet<ResidenceHistory> ResidenceHistories { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<FeeCategory> FeeCategories { get; set; }
    public DbSet<FeeCollection> FeeCollections { get; set; }
    public DbSet<FeeCharge> FeeCharges { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentDetail> PaymentDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USER
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        // APARTMENT
        modelBuilder.Entity<Apartment>()
            .HasIndex(a => a.ApartmentCode).IsUnique();

        modelBuilder.Entity<Apartment>()
            .Property(a => a.Area).HasPrecision(10, 2);

        // HOUSEHOLD
        modelBuilder.Entity<Household>()
            .HasIndex(h => h.HouseholdCode).IsUnique();

        modelBuilder.Entity<Household>()
            .HasOne(h => h.Apartment)
            .WithMany(a => a.Households)
            .HasForeignKey(h => h.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // RESIDENT
        modelBuilder.Entity<Resident>()
            .HasOne(r => r.Household)
            .WithMany(h => h.Residents)
            .HasForeignKey(r => r.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        // RESIDENCE HISTORY
        modelBuilder.Entity<ResidenceHistory>()
            .HasOne(r => r.Resident)
            .WithMany(r => r.ResidenceHistories)
            .HasForeignKey(r => r.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResidenceHistory>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // VEHICLE
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Apartment)
            .WithMany(a => a.Vehicles)
            .HasForeignKey(v => v.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Resident)
            .WithMany(r => r.Vehicles)
            .HasForeignKey(v => v.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FEE CATEGORY
        modelBuilder.Entity<FeeCategory>()
            .Property(f => f.UnitPrice).HasPrecision(18, 2);

        // FEE COLLECTION
        modelBuilder.Entity<FeeCollection>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // FEE CHARGE
        modelBuilder.Entity<FeeCharge>()
            .HasOne(f => f.FeeCollection)
            .WithMany(c => c.FeeCharges)
            .HasForeignKey(f => f.FeeCollectionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FeeCharge>()
            .HasOne(f => f.Apartment)
            .WithMany()
            .HasForeignKey(f => f.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FeeCharge>()
            .HasOne(f => f.FeeCategory)
            .WithMany(c => c.FeeCharges)
            .HasForeignKey(f => f.FeeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FeeCharge>()
            .Property(f => f.Quantity).HasPrecision(18, 2);

        modelBuilder.Entity<FeeCharge>()
            .Property(f => f.UnitPrice).HasPrecision(18, 2);

        modelBuilder.Entity<FeeCharge>()
            .Property(f => f.Amount).HasPrecision(18, 2);

        modelBuilder.Entity<FeeCharge>()
            .Property(f => f.PaidAmount).HasPrecision(18, 2);

        // PAYMENT
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Apartment)
            .WithMany()
            .HasForeignKey(p => p.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Resident)
            .WithMany()
            .HasForeignKey(p => p.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.ReceiptNumber).IsUnique();

        modelBuilder.Entity<Payment>()
            .Property(p => p.TotalAmount).HasPrecision(18, 2);

        // PAYMENT DETAIL
        modelBuilder.Entity<PaymentDetail>()
            .HasOne(d => d.Payment)
            .WithMany(p => p.PaymentDetails)
            .HasForeignKey(d => d.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentDetail>()
            .HasOne(d => d.FeeCharge)
            .WithMany(f => f.PaymentDetails)
            .HasForeignKey(d => d.FeeChargeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentDetail>()
            .Property(d => d.Amount).HasPrecision(18, 2);
    }
}