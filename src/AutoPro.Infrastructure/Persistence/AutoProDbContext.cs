using AutoPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Persistence;

public class AutoProDbContext(DbContextOptions<AutoProDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<ServiceJob> ServiceJobs => Set<ServiceJob>();
    public DbSet<JobPurchaseLine> JobPurchaseLines => Set<JobPurchaseLine>();
    public DbSet<JobStatusHistory> JobStatusHistories => Set<JobStatusHistory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<JobTypeServicePrice> JobTypeServicePrices => Set<JobTypeServicePrice>();
    public DbSet<JobInvoice> JobInvoices => Set<JobInvoice>();
    public DbSet<JobInvoiceLineItem> JobInvoiceLineItems => Set<JobInvoiceLineItem>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(32).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasIndex(x => x.Phone);
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.QrToken).HasMaxLength(64).IsRequired();
            e.Property(x => x.PlatePrefix).HasMaxLength(8).IsRequired();
            e.Property(x => x.PlateSuffix).HasMaxLength(8).IsRequired();
            e.Property(x => x.PlateNumber).HasMaxLength(32).IsRequired();
            e.Property(x => x.Vin).HasMaxLength(32);
            e.Property(x => x.Make).HasMaxLength(64);
            e.Property(x => x.Model).HasMaxLength(64);
            e.HasIndex(x => x.QrToken).IsUnique();
            e.HasIndex(x => x.PlateNumber);
            e.HasIndex(x => new { x.PlatePrefix, x.PlateSuffix }).IsUnique();
            e.HasOne(x => x.Customer).WithMany(c => c.Vehicles).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<StaffUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserName).HasMaxLength(64).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<ServiceJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.JobNumber).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.JobNumber).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAtUtc);
            e.Property(x => x.JobTypeOtherDescription).HasMaxLength(200);
            e.Property(x => x.CustomServiceChargeLkr).HasPrecision(18, 2);
            e.Property(x => x.StoreCustomerName).HasMaxLength(200);
            e.HasOne(x => x.Vehicle).WithMany(v => v.Jobs).HasForeignKey(x => x.VehicleId).IsRequired(false);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
            e.HasOne(x => x.Invoice).WithOne(i => i.ServiceJob).HasForeignKey<JobInvoice>(i => i.ServiceJobId);
        });

        modelBuilder.Entity<JobTypeServicePrice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ServiceChargeLkr).HasPrecision(18, 2);
            e.HasIndex(x => x.JobType).IsUnique();
        });

        modelBuilder.Entity<OrganizationSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Tagline).HasMaxLength(200);
            e.Property(x => x.LogoContentType).HasMaxLength(64);
        });

        modelBuilder.Entity<JobInvoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ServiceJobId).IsUnique();
        });

        modelBuilder.Entity<JobInvoiceLineItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(512).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasOne(x => x.JobInvoice).WithMany(i => i.LineItems).HasForeignKey(x => x.JobInvoiceId);
            e.HasOne(x => x.JobPurchaseLine).WithMany().HasForeignKey(x => x.JobPurchaseLineId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PartNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.QuantityOnHand).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(x => x.PartNumber);
            e.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<JobPurchaseLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(512).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.VendorName).HasMaxLength(200);
            e.HasOne(x => x.ServiceJob).WithMany(j => j.PurchaseLines).HasForeignKey(x => x.ServiceJobId);
            e.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.InventoryItemId);
        });

        modelBuilder.Entity<JobStatusHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.ServiceJob).WithMany(j => j.StatusHistory).HasForeignKey(x => x.ServiceJobId);
        });
    }
}
