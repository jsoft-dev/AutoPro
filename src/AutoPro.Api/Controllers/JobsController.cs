using System.Security.Claims;
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
[Authorize(Roles = "CustomerRepresentative")]
[Route("api/v1/jobs")]
public class JobsController(AutoProDbContext db, JobNumberGenerator jobNumbers) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("today")]
    public async Task<ActionResult<IEnumerable<JobListItemResponse>>> Today(CancellationToken ct)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);
        return Ok(await ListInternal(start, end, ct));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobListItemResponse>>> List(
        [FromQuery] JobStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var q = db.ServiceJobs.AsNoTracking()
            .Include(j => j.Vehicle).ThenInclude(v => v.Customer)
            .AsQueryable();

        if (status.HasValue) q = q.Where(j => j.Status == status.Value);
        if (from.HasValue) q = q.Where(j => j.CreatedAtUtc >= from.Value.ToUniversalTime());
        if (to.HasValue) q = q.Where(j => j.CreatedAtUtc < to.Value.ToUniversalTime());

        var items = await q
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(100)
            .Select(j => new JobListItemResponse(
                j.Id,
                j.JobNumber,
                j.Status,
                j.Vehicle.PlateNumber,
                j.Vehicle.Customer.FullName,
                j.OdometerReading,
                j.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var job = await LoadJobForDetailAsync(id, ct);
        if (job is null) return NotFound();
        return Ok(ToDetail(job));
    }

    [HttpPost]
    public async Task<ActionResult<JobDetailResponse>> TakeJob([FromBody] TakeJobRequest request, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (vehicle is null) return NotFound(new { message = "Vehicle not found." });

        var active = await db.ServiceJobs.AnyAsync(
            j => j.VehicleId == request.VehicleId &&
                 (j.Status == JobStatus.OnGoing || j.Status == JobStatus.OnHold),
            ct);

        if (active)
            return Conflict(new { message = "This vehicle already has an active job." });

        var job = new ServiceJob
        {
            Id = Guid.NewGuid(),
            JobNumber = await jobNumbers.NextAsync(ct),
            VehicleId = vehicle.Id,
            CreatedByUserId = CurrentUserId,
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

        db.ServiceJobs.Add(job);
        await db.SaveChangesAsync(ct);

        job.Vehicle = vehicle;
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToDetail(job));
    }

    [HttpPatch("{id:guid}/status")]
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
            job.CompletedAtUtc = DateTime.UtcNow;

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
        return detail is null ? NotFound() : Ok(ToDetail(detail));
    }

    [HttpPost("{id:guid}/purchases")]
    public async Task<ActionResult<JobDetailResponse>> AddPurchase(
        Guid id,
        [FromBody] AddPurchaseLineRequest request,
        CancellationToken ct)
    {
        var job = await db.ServiceJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job is null) return NotFound();
        if (!JobStatusRules.CanModifyPurchases(job.Status))
            return BadRequest(new { message = "Purchases can only be added while the job is On Going or On Hold." });

        if (request.Source == PurchaseSource.ExternalVendor && string.IsNullOrWhiteSpace(request.VendorName))
            return BadRequest(new { message = "VendorName is required for external vendor purchases." });

        db.JobPurchaseLines.Add(new JobPurchaseLine
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            Source = request.Source,
            InventoryItemId = request.InventoryItemId,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            VendorName = request.VendorName?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            AddedByUserId = CurrentUserId
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
        return detail is null ? NotFound() : Ok(ToDetail(detail));
    }

    private Task<ServiceJob?> LoadJobForDetailAsync(Guid id, CancellationToken ct) =>
        db.ServiceJobs.AsNoTracking()
            .Include(j => j.Vehicle).ThenInclude(v => v.Customer)
            .Include(j => j.PurchaseLines)
            .Include(j => j.StatusHistory)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

    private async Task<List<JobListItemResponse>> ListInternal(DateTime from, DateTime to, CancellationToken ct) =>
        await db.ServiceJobs.AsNoTracking()
            .Include(j => j.Vehicle).ThenInclude(v => v.Customer)
            .Where(j => j.CreatedAtUtc >= from && j.CreatedAtUtc < to)
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => new JobListItemResponse(
                j.Id,
                j.JobNumber,
                j.Status,
                j.Vehicle.PlateNumber,
                j.Vehicle.Customer.FullName,
                j.OdometerReading,
                j.CreatedAtUtc))
            .ToListAsync(ct);

    private static JobDetailResponse ToDetail(ServiceJob job) =>
        new(
            job.Id,
            job.JobNumber,
            job.Status,
            job.VehicleId,
            job.Vehicle.PlateNumber,
            job.Vehicle.CustomerId,
            job.Vehicle.Customer.FullName,
            job.OdometerReading,
            job.Notes,
            job.CreatedAtUtc,
            job.PurchaseLines
                .OrderBy(p => p.CreatedAtUtc)
                .Select(p => new JobPurchaseLineResponse(
                    p.Id, p.Source, p.Description, p.Quantity, p.UnitPrice, p.VendorName, p.CreatedAtUtc))
                .ToList(),
            job.StatusHistory
                .OrderBy(h => h.ChangedAtUtc)
                .Select(h => new JobStatusHistoryResponse(h.FromStatus, h.ToStatus, h.ChangedAtUtc, h.Note))
                .ToList());
}
