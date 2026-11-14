using System.Security.Claims;
using AutoPro.Application.Billing;
using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.Jobs;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobs")]
public class JobsController(
    AutoProDbContext db,
    JobNumberGenerator jobNumbers,
    IJobInvoiceFinalizer invoiceFinalizer) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("today")]
    [Authorize(Roles = "CustomerRepresentative,AdministrativeOfficer")]
    public async Task<ActionResult<IEnumerable<JobListItemResponse>>> Today(CancellationToken ct)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);
        return Ok(await ListInternal(start, end, ct));
    }

    [HttpGet]
    [Authorize(Roles = "CustomerRepresentative,AdministrativeOfficer")]
    public async Task<ActionResult<PagedResult<JobListItemResponse>>> List(
        [FromQuery] JobStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool inWorkshop = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.ServiceJobs.AsNoTracking()
            .Include(j => j.Vehicle).ThenInclude(v => v!.Customer)
            .Where(j => j.JobType != JobType.StoreSale)
            .AsQueryable();

        if (status.HasValue) q = q.Where(j => j.Status == status.Value);
        if (inWorkshop)
        {
            q = q.Where(j => j.Status == JobStatus.OnGoing || j.Status == JobStatus.OnHold);
        }

        if (from.HasValue) q = q.Where(j => j.CreatedAtUtc >= from.Value.ToUniversalTime());
        if (to.HasValue) q = q.Where(j => j.CreatedAtUtc < to.Value.ToUniversalTime());

        var totalCount = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(j => j.CreatedAtUtc)
            .ThenByDescending(j => j.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id,
                j.JobNumber,
                j.Status,
                j.JobType,
                j.JobTypeOtherDescription,
                PlateNumber = j.Vehicle!.PlateNumber,
                CustomerName = j.Vehicle.Customer.FullName,
                j.OdometerReading,
                j.CreatedAtUtc
            })
            .ToListAsync(ct);

        var items = rows.Select(j => new JobListItemResponse(
            j.Id,
            j.JobNumber,
            j.Status,
            j.JobType,
            j.JobTypeOtherDescription,
            JobTypeDisplay.Label(j.JobType, j.JobTypeOtherDescription),
            j.PlateNumber,
            j.CustomerName,
            j.OdometerReading,
            j.CreatedAtUtc)).ToList();

        return Ok(new PagedResult<JobListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "CustomerRepresentative,AdministrativeOfficer")]
    public async Task<ActionResult<JobDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var job = await LoadJobForDetailAsync(id, ct);
        if (job is null) return NotFound();
        return Ok(await MapDetailAsync(job, ct));
    }

    [HttpPost]
    [Authorize(Roles = "CustomerRepresentative")]
    public async Task<ActionResult<JobDetailResponse>> TakeJob([FromBody] TakeJobRequest request, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (vehicle is null) return NotFound(new { message = "Vehicle not found." });

        var otherError = JobOtherTypeRules.Validate(
            request.JobType, request.JobTypeOtherDescription, request.CustomServiceChargeLkr);
        if (otherError is not null)
            return BadRequest(new { message = otherError });

        var lastOdometer = await VehicleOdometerLookup.GetLastRecordedAsync(db, vehicle.Id, excludeJobId: null, ct);
        var odometerError = VehicleOdometerRules.ValidateNewReading(request.OdometerReading, lastOdometer);
        if (odometerError is not null)
            return BadRequest(new { message = odometerError });

        var job = new ServiceJob
        {
            Id = Guid.NewGuid(),
            JobNumber = await jobNumbers.NextAsync(ct),
            VehicleId = vehicle.Id,
            CreatedByUserId = CurrentUserId,
            JobType = request.JobType,
            JobTypeOtherDescription = request.JobType == JobType.Other
                ? request.JobTypeOtherDescription!.Trim()
                : null,
            CustomServiceChargeLkr = request.JobType == JobType.Other
                ? request.CustomServiceChargeLkr
                : null,
            OdometerReading = request.OdometerReading,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = JobStatus.OnGoing,
            CreatedAtUtc = DateTime.UtcNow
        };

        job.StatusHistory.Add(new JobStatusHistory
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            FromStatus = JobStatus.OnGoing,
            ToStatus = JobStatus.OnGoing,
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Note = "Job taken at front desk."
        });

        var invoice = new JobInvoice
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            ServiceJob = job,
            FinalizedAtUtc = job.CreatedAtUtc,
        };
        job.Invoice = invoice;

        db.ServiceJobs.Add(job);
        db.JobInvoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        job.Vehicle = vehicle;
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, await MapDetailAsync(job, ct));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "CustomerRepresentative")]
    public async Task<ActionResult<JobDetailResponse>> UpdateJob(
        Guid id,
        [FromBody] UpdateJobRequest request,
        CancellationToken ct)
    {
        var job = await db.ServiceJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();

        if (!JobStatusRules.CanModifyJobDetails(job.Status))
            return BadRequest(new { message = "Job type and odometer can only be changed while the job is On Going or On Hold." });

        var otherError = JobOtherTypeRules.Validate(
            request.JobType, request.JobTypeOtherDescription, request.CustomServiceChargeLkr);
        if (otherError is not null)
            return BadRequest(new { message = otherError });

        if (job.VehicleId is Guid vehicleId)
        {
            var lastOdometer = await VehicleOdometerLookup.GetLastRecordedAsync(db, vehicleId, job.Id, ct);
            var odometerError = VehicleOdometerRules.ValidateNewReading(request.OdometerReading, lastOdometer);
            if (odometerError is not null)
                return BadRequest(new { message = odometerError });
        }
        else if (request.OdometerReading is < 0)
        {
            return BadRequest(new { message = "Odometer cannot be negative." });
        }

        job.JobType = request.JobType;
        job.JobTypeOtherDescription = request.JobType == JobType.Other
            ? request.JobTypeOtherDescription!.Trim()
            : null;
        job.CustomServiceChargeLkr = request.JobType == JobType.Other
            ? request.CustomServiceChargeLkr
            : null;
        job.OdometerReading = request.OdometerReading;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This job was changed by someone else. Refresh and try again." });
        }

        var detail = await LoadJobForDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(await MapDetailAsync(detail, ct));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "CustomerRepresentative")]
    public async Task<ActionResult<JobDetailResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateJobStatusRequest request,
        CancellationToken ct)
    {
        if (request.Status == JobStatus.Closed)
            return Forbid();

        // Track only the job row — loading Vehicle/Customer/PurchaseLines can mark unrelated
        // entities as Modified and cause SaveChanges to issue updates that match 0 rows.
        var job = await db.ServiceJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job is null) return NotFound();

        if (!JobStatusRules.CanCustomerRepresentativeChange(job.Status, request.Status))
            return BadRequest(new { message = $"Cannot change status from {job.Status} to {request.Status}." });

        var from = job.Status;
        job.Status = request.Status;
        if (request.Status == JobStatus.Completed)
        {
            job.CompletedAtUtc = DateTime.UtcNow;
            await invoiceFinalizer.EnsureServiceChargeOnCompletionAsync(job.Id, ct);
        }

        db.JobStatusHistories.Add(new JobStatusHistory
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            FromStatus = from,
            ToStatus = request.Status,
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Note = request.Note
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This job was changed by someone else. Refresh and try again." });
        }

        var detail = await LoadJobForDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(await MapDetailAsync(detail, ct));
    }

    [HttpPost("{id:guid}/purchases")]
    [Authorize(Roles = "CustomerRepresentative,AdministrativeOfficer")]
    public async Task<ActionResult<JobDetailResponse>> AddPurchase(
        Guid id,
        [FromBody] AddPurchaseLineRequest request,
        CancellationToken ct)
    {
        var job = await db.ServiceJobs
            .Include(j => j.Invoice!).ThenInclude(i => i.LineItems)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job is null) return NotFound();

        var isAdmin = User.IsInRole("AdministrativeOfficer");
        if (isAdmin)
        {
            if (!JobStatusRules.CanAdministrativeOfficerModifyPurchases(job.Status))
                return BadRequest(new { message = "Purchases can only be added while the job is open or awaiting payment." });
        }
        else if (!JobStatusRules.CanModifyPurchases(job.Status))
        {
            return BadRequest(new { message = "Purchases can only be added while the job is On Going or On Hold." });
        }

        if (request.Source == PurchaseSource.ExternalVendor && string.IsNullOrWhiteSpace(request.VendorName))
            return BadRequest(new { message = "VendorName is required for external vendor purchases." });

        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero." });

        InventoryItem? inventoryItem = null;
        if (request.Source == PurchaseSource.Inventory)
        {
            if (request.InventoryItemId is null)
                return BadRequest(new { message = "Select a part from shop inventory." });

            inventoryItem = await db.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId.Value && i.IsActive, ct);
            if (inventoryItem is null)
                return BadRequest(new { message = "That inventory part was not found or is inactive." });
            if (inventoryItem.QuantityOnHand < request.Quantity)
                return BadRequest(new
                {
                    message = $"Insufficient stock for {inventoryItem.PartNumber}. On hand: {inventoryItem.QuantityOnHand}."
                });
        }

        var description = request.Description.Trim();
        if (string.IsNullOrEmpty(description) && inventoryItem is not null)
            description = inventoryItem.Name;
        if (string.IsNullOrEmpty(description))
            return BadRequest(new { message = "Description is required." });

        var unitPrice = request.UnitPrice;
        if (unitPrice < 0)
            return BadRequest(new { message = "Unit price cannot be negative." });
        if (unitPrice == 0 && inventoryItem is not null)
            unitPrice = inventoryItem.UnitPrice;

        if (inventoryItem is not null)
        {
            inventoryItem.QuantityOnHand -= request.Quantity;
            inventoryItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        var purchaseLine = new JobPurchaseLine
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            Source = request.Source,
            InventoryItemId = request.Source == PurchaseSource.Inventory ? request.InventoryItemId : null,
            Description = description,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            VendorName = request.Source == PurchaseSource.ExternalVendor ? request.VendorName?.Trim() : null,
            CreatedAtUtc = DateTime.UtcNow,
            AddedByUserId = CurrentUserId
        };
        db.JobPurchaseLines.Add(purchaseLine);

        try
        {
            await db.SaveChangesAsync(ct);
            await invoiceFinalizer.AddPurchaseLineAsync(job.Id, purchaseLine.Id, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This job was changed by someone else. Refresh and try again." });
        }

        var detail = await LoadJobForDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(await MapDetailAsync(detail, ct));
    }

    private Task<ServiceJob?> LoadJobForDetailAsync(Guid id, CancellationToken ct) =>
        db.ServiceJobs.AsNoTracking()
            .Include(j => j.Vehicle).ThenInclude(v => v!.Customer)
            .Include(j => j.PurchaseLines).ThenInclude(p => p.InventoryItem)
            .Include(j => j.Invoice!).ThenInclude(i => i.LineItems)
                .ThenInclude(l => l.JobPurchaseLine!).ThenInclude(p => p.InventoryItem)
            .Include(j => j.StatusHistory)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

    private async Task<JobDetailResponse> MapDetailAsync(ServiceJob job, CancellationToken ct)
    {
        var serviceCharge = await JobTypeServicePriceLookup.GetEffectiveChargeAsync(db, job, ct);
        var lastOdometer = job.VehicleId is Guid vehicleId
            ? await VehicleOdometerLookup.GetLastRecordedAsync(db, vehicleId, job.Id, ct)
            : null;
        return JobDetailMapper.ToDetail(job, serviceCharge, lastOdometer);
    }

    private async Task<List<JobListItemResponse>> ListInternal(DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await db.ServiceJobs.AsNoTracking()
            .Include(j => j.Vehicle).ThenInclude(v => v!.Customer)
            .Where(j => j.JobType != JobType.StoreSale)
            .Where(j => j.CreatedAtUtc >= from && j.CreatedAtUtc < to)
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => new
            {
                j.Id,
                j.JobNumber,
                j.Status,
                j.JobType,
                j.JobTypeOtherDescription,
                PlateNumber = j.Vehicle!.PlateNumber,
                CustomerName = j.Vehicle.Customer.FullName,
                j.OdometerReading,
                j.CreatedAtUtc
            })
            .ToListAsync(ct);

        return rows.Select(j => new JobListItemResponse(
            j.Id,
            j.JobNumber,
            j.Status,
            j.JobType,
            j.JobTypeOtherDescription,
            JobTypeDisplay.Label(j.JobType, j.JobTypeOtherDescription),
            j.PlateNumber,
            j.CustomerName,
            j.OdometerReading,
            j.CreatedAtUtc)).ToList();
    }

}
