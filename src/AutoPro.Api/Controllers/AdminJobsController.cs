using System.Security.Claims;
using AutoPro.Application.Billing;
using AutoPro.Application.Contracts.Jobs;
using AutoPro.Application.Messaging;
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
[Route("api/v1/admin/jobs")]
public class AdminJobsController(
    AutoProDbContext db,
    IJobInvoiceFinalizer invoiceFinalizer,
    IPaymentClosedNotifier paymentClosedNotifier,
    ILogger<AdminJobsController> logger) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<JobDetailResponse>> Close(
        Guid id,
        [FromBody] CloseJobRequest? request,
        CancellationToken ct)
    {
        var job = await db.ServiceJobs
            .Include(j => j.Vehicle).ThenInclude(v => v!.Customer)
            .Include(j => j.PurchaseLines)
            .Include(j => j.Invoice!).ThenInclude(i => i.LineItems)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();

        if (!JobStatusRules.CanAdministrativeOfficerClose(job.Status))
            return BadRequest(new { message = "Only completed jobs can be closed after payment." });

        await invoiceFinalizer.FinalizeForJobAsync(job.Id, ct);

        var from = job.Status;
        job.Status = JobStatus.Closed;
        job.ClosedAtUtc = DateTime.UtcNow;

        var paymentMethod = string.IsNullOrWhiteSpace(request?.PaymentNote)
            ? null
            : request.PaymentNote.Trim();

        if (!PaymentMethodLabels.IsAllowed(paymentMethod))
            return BadRequest(new { message = "Select a payment method: Cash, Card, or Bank Transfer." });

        db.JobStatusHistories.Add(new JobStatusHistory
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            FromStatus = from,
            ToStatus = JobStatus.Closed,
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Note = paymentMethod!,
        });

        try
        {
            await db.SaveChangesAsync(ct);
            await invoiceFinalizer.StampFinalizedAtCloseAsync(job.Id, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This job was changed by someone else. Refresh and try again." });
        }

        var paymentTotal = job.Invoice?.LineItems.Sum(l => l.Quantity * l.UnitPrice)
            ?? job.PurchaseLines.Sum(p => p.Quantity * p.UnitPrice);
        try
        {
            await paymentClosedNotifier.NotifyPaymentReceivedAsync(job, paymentTotal, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Payment thank-you email failed for job {JobId}; job close succeeded.",
                job.Id);
        }

        var detail = await LoadJobForDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(await MapDetailAsync(detail, ct));
    }

    /// <summary>
    /// Clears purchases (restores inventory stock), sets the job back to On Going,
    /// and trims status history so the full CR → payment flow can be run again.
    /// </summary>
    [HttpPost("{id:guid}/reset-workflow")]
    public async Task<ActionResult<JobDetailResponse>> ResetWorkflow(Guid id, CancellationToken ct)
    {
        var job = await db.ServiceJobs
            .Include(j => j.PurchaseLines)
            .Include(j => j.Invoice!).ThenInclude(i => i.LineItems)
            .Include(j => j.StatusHistory)
            .FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();

        if (job.Invoice is not null)
            db.JobInvoiceLineItems.RemoveRange(job.Invoice.LineItems);
        else
            await invoiceFinalizer.EnsureForJobAsync(job.Id, ct);

        foreach (var line in job.PurchaseLines.ToList())
        {
            if (line.Source == PurchaseSource.Inventory && line.InventoryItemId is Guid itemId)
            {
                var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
                if (item is not null)
                {
                    item.QuantityOnHand += line.Quantity;
                    item.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            db.JobPurchaseLines.Remove(line);
        }

        job.Status = JobStatus.OnGoing;
        job.CompletedAtUtc = null;
        job.ClosedAtUtc = null;

        var initialNote = job.JobType == JobType.StoreSale
            ? "Counter sale opened."
            : "Job taken at front desk.";

        var initialEntry = job.StatusHistory
            .OrderBy(h => h.ChangedAtUtc)
            .FirstOrDefault(h =>
                h.FromStatus == JobStatus.OnGoing &&
                h.ToStatus == JobStatus.OnGoing &&
                h.Note == initialNote);

        foreach (var entry in job.StatusHistory.ToList())
        {
            if (initialEntry is not null && entry.Id == initialEntry.Id)
                continue;
            db.JobStatusHistories.Remove(entry);
        }

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
}
