using AutoPro.Application.Vehicles;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoPro.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task EnsureDemoDataAsync(
        AutoProDbContext db,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        if (await db.Customers.AnyAsync(c => c.Id == DemoDataIds.MarkerCustomerId, ct))
        {
            logger?.LogInformation("Demo customers, vehicles, suppliers, and jobs already present.");
            return;
        }

        if (!await db.StaffUsers.AnyAsync(u => u.Id == DemoDataIds.DemoCrUserId, ct))
        {
            logger?.LogWarning("Skipping demo business seed: demo staff user cr.demo is not present.");
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var createdAt = DemoMockData.AnchorUtc;
        SeedSuppliers(db, createdAt);
        SeedCustomers(db, createdAt);
        SeedVehicles(db, createdAt);
        await SeedJobsAsync(db, ct);
        await LinkMockInventoryToSuppliersAsync(db, ct);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger?.LogInformation(
            "Seeded demo data: {Customers} customers, {Vehicles} vehicles, {Suppliers} suppliers, {Jobs} jobs.",
            DemoMockData.Customers.Count,
            DemoMockData.Vehicles.Count,
            DemoMockData.Suppliers.Count,
            DemoMockData.Jobs.Count);
    }

    private static void SeedSuppliers(AutoProDbContext db, DateTime createdAtUtc)
    {
        foreach (var row in DemoMockData.Suppliers)
        {
            db.Suppliers.Add(new Supplier
            {
                Id = DemoDataIds.EntityId(100 + row.Index),
                Name = row.Name,
                ContactPerson = row.ContactPerson,
                Phone = row.Phone,
                IsActive = true,
                CreatedAtUtc = createdAtUtc,
            });
        }
    }

    private static void SeedCustomers(AutoProDbContext db, DateTime createdAtUtc)
    {
        foreach (var row in DemoMockData.Customers)
        {
            db.Customers.Add(new Customer
            {
                Id = DemoDataIds.EntityId(row.Index),
                FullName = row.FullName,
                Phone = row.Phone,
                Email = row.Email,
                CreatedAtUtc = createdAtUtc,
            });
        }
    }

    private static void SeedVehicles(AutoProDbContext db, DateTime createdAtUtc)
    {
        foreach (var row in DemoMockData.Vehicles)
        {
            var plateNumber = PlateNumber.Format(row.PlatePrefix, row.PlateSuffix);
            db.Vehicles.Add(new Vehicle
            {
                Id = DemoDataIds.EntityId(200 + row.Index),
                CustomerId = DemoDataIds.EntityId(row.CustomerIndex),
                QrToken = row.QrToken,
                PlatePrefix = row.PlatePrefix,
                PlateSuffix = row.PlateSuffix,
                PlateNumber = plateNumber,
                Make = row.Make,
                Model = row.Model,
                Year = row.Year,
                CreatedAtUtc = createdAtUtc,
            });
        }
    }

    private static async Task SeedJobsAsync(AutoProDbContext db, CancellationToken ct)
    {
        foreach (var row in DemoMockData.Jobs)
        {
            var createdAt = DemoMockData.CreatedAt(row.DaysAgo);
            var jobId = DemoDataIds.EntityId(300 + row.Index);

            var job = BuildJob(row, jobId, createdAt);
            db.ServiceJobs.Add(job);
            AddInitialStatusHistory(db, job, row.Index, createdAt);
            AddStatusHistoryForTerminalState(db, job, row, createdAt);

            if (row.WithInventoryPurchase)
                await AddDemoPurchaseLineAsync(db, job, row.Index, createdAt, ct);
        }
    }

    private static ServiceJob BuildJob(DemoMockData.JobRow row, Guid jobId, DateTime createdAt)
    {
        var completedAt = row.Status is JobStatus.Completed or JobStatus.Closed
            ? createdAt.AddHours(6)
            : (DateTime?)null;
        var closedAt = row.Status == JobStatus.Closed
            ? createdAt.AddHours(8)
            : (DateTime?)null;

        return new ServiceJob
        {
            Id = jobId,
            JobNumber = row.JobNumber,
            VehicleId = row.VehicleIndex is int vehicleIndex
                ? DemoDataIds.EntityId(200 + vehicleIndex)
                : null,
            StoreCustomerName = row.StoreCustomerName,
            CreatedByUserId = DemoDataIds.DemoCrUserId,
            OdometerReading = row.OdometerKm,
            JobType = row.JobType,
            JobTypeOtherDescription = row.JobTypeOtherDescription,
            CustomServiceChargeLkr = row.CustomServiceChargeLkr,
            Status = row.Status,
            Notes = row.Notes,
            CreatedAtUtc = createdAt,
            CompletedAtUtc = completedAt,
            ClosedAtUtc = closedAt,
        };
    }

    private static void AddInitialStatusHistory(
        AutoProDbContext db,
        ServiceJob job,
        int jobIndex,
        DateTime createdAt)
    {
        db.JobStatusHistories.Add(new JobStatusHistory
        {
            Id = DemoDataIds.EntityId(4000 + jobIndex),
            ServiceJobId = job.Id,
            FromStatus = JobStatus.OnGoing,
            ToStatus = JobStatus.OnGoing,
            ChangedByUserId = DemoDataIds.DemoCrUserId,
            ChangedAtUtc = createdAt,
            Note = job.JobType == JobType.StoreSale ? "Store sale opened." : "Job taken at front desk (demo seed).",
        });
    }

    private static void AddStatusHistoryForTerminalState(
        AutoProDbContext db,
        ServiceJob job,
        DemoMockData.JobRow row,
        DateTime createdAt)
    {
        if (row.Status == JobStatus.OnHold)
        {
            db.JobStatusHistories.Add(new JobStatusHistory
            {
                Id = DemoDataIds.EntityId(4100 + row.Index),
                ServiceJobId = job.Id,
                FromStatus = JobStatus.OnGoing,
                ToStatus = JobStatus.OnHold,
                ChangedByUserId = DemoDataIds.DemoCrUserId,
                ChangedAtUtc = createdAt.AddHours(2),
                Note = "Awaiting parts (demo).",
            });
            return;
        }

        if (row.Status == JobStatus.Cancelled)
        {
            db.JobStatusHistories.Add(new JobStatusHistory
            {
                Id = DemoDataIds.EntityId(4200 + row.Index),
                ServiceJobId = job.Id,
                FromStatus = JobStatus.OnGoing,
                ToStatus = JobStatus.Cancelled,
                ChangedByUserId = DemoDataIds.DemoCrUserId,
                ChangedAtUtc = createdAt.AddHours(1),
                Note = row.Notes,
            });
            return;
        }

        if (row.Status == JobStatus.Completed || row.Status == JobStatus.Closed)
        {
            db.JobStatusHistories.Add(new JobStatusHistory
            {
                Id = DemoDataIds.EntityId(4300 + row.Index),
                ServiceJobId = job.Id,
                FromStatus = JobStatus.OnGoing,
                ToStatus = JobStatus.Completed,
                ChangedByUserId = DemoDataIds.DemoAoUserId,
                ChangedAtUtc = createdAt.AddHours(5),
                Note = "Work completed (demo).",
            });
        }

        if (row.Status == JobStatus.Closed)
        {
            db.JobStatusHistories.Add(new JobStatusHistory
            {
                Id = DemoDataIds.EntityId(4400 + row.Index),
                ServiceJobId = job.Id,
                FromStatus = JobStatus.Completed,
                ToStatus = JobStatus.Closed,
                ChangedByUserId = DemoDataIds.DemoAoUserId,
                ChangedAtUtc = createdAt.AddHours(8),
                Note = "Payment received (demo).",
            });
        }
    }

    private static async Task AddDemoPurchaseLineAsync(
        AutoProDbContext db,
        ServiceJob job,
        int jobIndex,
        DateTime createdAt,
        CancellationToken ct)
    {
        var inventoryIndex = (jobIndex % 20) + 1;
        var inventoryId = InventoryMockData.DeterministicId(inventoryIndex);
        var item = await db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inventoryId, ct);
        if (item is null)
            return;

        db.JobPurchaseLines.Add(new JobPurchaseLine
        {
            Id = DemoDataIds.EntityId(500 + jobIndex),
            ServiceJobId = job.Id,
            Source = PurchaseSource.Inventory,
            InventoryItemId = item.Id,
            Description = item.Name,
            Quantity = 1,
            UnitPrice = item.UnitPrice,
            CreatedAtUtc = createdAt.AddMinutes(30),
            AddedByUserId = DemoDataIds.DemoCrUserId,
        });
    }

    private static async Task LinkMockInventoryToSuppliersAsync(AutoProDbContext db, CancellationToken ct)
    {
        var supplierIds = DemoMockData.Suppliers
            .Select(s => DemoDataIds.EntityId(100 + s.Index))
            .ToList();

        var mockItems = await db.InventoryItems
            .Where(i => i.PartNumber.StartsWith(InventoryMockData.PartNumberPrefix) && i.SupplierId == null)
            .OrderBy(i => i.PartNumber)
            .Take(30)
            .ToListAsync(ct);

        for (var i = 0; i < mockItems.Count; i++)
        {
            mockItems[i].SupplierId = supplierIds[i % supplierIds.Count];
            mockItems[i].UpdatedAtUtc = DemoMockData.AnchorUtc;
        }
    }
}