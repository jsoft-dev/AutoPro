using System.Security.Claims;
using AutoPro.Application.Billing;
using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.Jobs;
using AutoPro.Application.Contracts.Sales;
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
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/sales")]
public class AdminSalesController(
    AutoProDbContext db,
    JobNumberGenerator jobNumbers,
    IJobInvoiceFinalizer invoiceFinalizer) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<JobListItemResponse>>> List(
        [FromQuery] JobStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.ServiceJobs.AsNoTracking()
            .Where(j => j.JobType == JobType.StoreSale);

        if (status.HasValue)
            q = q.Where(j => j.Status == status.Value);

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
                j.StoreCustomerName,
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
            null,
            string.IsNullOrWhiteSpace(j.StoreCustomerName) ? "Walk-in" : j.StoreCustomerName.Trim(),
            j.OdometerReading,
            j.CreatedAtUtc)).ToList();

        return Ok(new PagedResult<JobListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpPost]
    public async Task<ActionResult<JobDetailResponse>> Create(
        [FromBody] CreateStoreSaleRequest? request,
        CancellationToken ct)
    {
        var storeCustomerName = string.IsNullOrWhiteSpace(request?.StoreCustomerName)
            ? null
            : request.StoreCustomerName.Trim();
        var notes = string.IsNullOrWhiteSpace(request?.Notes) ? null : request.Notes.Trim();

        var job = new ServiceJob
        {
            Id = Guid.NewGuid(),
            JobNumber = await jobNumbers.NextAsync(ct),
            JobType = JobType.StoreSale,
            VehicleId = null,
            StoreCustomerName = storeCustomerName,
            Status = JobStatus.OnGoing,
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = CurrentUserId,
        };

        db.ServiceJobs.Add(job);
        db.JobStatusHistories.Add(new JobStatusHistory
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            FromStatus = JobStatus.OnGoing,
            ToStatus = JobStatus.OnGoing,
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Note = "Counter sale opened.",
        });

        await db.SaveChangesAsync(ct);

        var detail = await LoadJobForDetailAsync(job.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, await MapDetailAsync(detail!, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var job = await LoadJobForDetailAsync(id, ct);
        if (job is null || job.JobType != JobType.StoreSale)
            return NotFound();

        return Ok(await MapDetailAsync(job, ct));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<JobDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateStoreSaleRequest request,
        CancellationToken ct)
    {
        var job = await db.ServiceJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null || job.JobType != JobType.StoreSale)
            return NotFound();

        if (job.Status != JobStatus.OnGoing && job.Status != JobStatus.OnHold)
            return BadRequest(new { message = "Only open counter sales can be edited." });

        job.StoreCustomerName = string.IsNullOrWhiteSpace(request.StoreCustomerName)
            ? null
            : request.StoreCustomerName.Trim();
        job.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This sale was changed by someone else. Refresh and try again." });
        }

        var detail = await LoadJobForDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(await MapDetailAsync(detail, ct));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<JobDetailResponse>> Complete(
        Guid id,
        [FromBody] CompleteStoreSaleRequest? request,
        CancellationToken ct)
    {
        var job = await db.ServiceJobs
            .Include(j => j.PurchaseLines)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job is null || job.JobType != JobType.StoreSale)
            return NotFound();

        if (job.Status != JobStatus.OnGoing && job.Status != JobStatus.OnHold)
            return BadRequest(new { message = "Only open counter sales can be completed." });

        if (job.PurchaseLines.Count == 0)
            return BadRequest(new { message = "Add at least one line before completing the sale." });

        var from = job.Status;
        job.Status = JobStatus.Completed;
        job.CompletedAtUtc = DateTime.UtcNow;
        await invoiceFinalizer.FinalizeForJobAsync(job.Id, ct);

        var note = string.IsNullOrWhiteSpace(request?.Note)
            ? "Counter sale completed."
            : request.Note.Trim();

        db.JobStatusHistories.Add(new JobStatusHistory
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            FromStatus = from,
            ToStatus = JobStatus.Completed,
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Note = note,
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This sale was changed by someone else. Refresh and try again." });
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
        return JobDetailMapper.ToDetail(job, serviceCharge, vehicleLastRecordedOdometerKm: null);
    }
}
