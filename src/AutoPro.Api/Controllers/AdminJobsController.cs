using System.Security.Claims;
using AutoPro.Application.Contracts.Jobs;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/jobs")]
public class AdminJobsController(AutoProDbContext db) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<JobDetailResponse>> Close(
        Guid id,
        [FromBody] CloseJobRequest? request,
        CancellationToken ct)
    {
        var job = await db.ServiceJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();

        if (!JobStatusRules.CanAdministrativeOfficerClose(job.Status))
            return BadRequest(new { message = "Only completed jobs can be closed after payment." });

        var from = job.Status;
        job.Status = JobStatus.Closed;
        job.ClosedAtUtc = DateTime.UtcNow;

        var paymentNote = string.IsNullOrWhiteSpace(request?.PaymentNote)
            ? null
            : request.PaymentNote.Trim();

        db.JobStatusHistories.Add(new JobStatusHistory
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            FromStatus = from,
            ToStatus = JobStatus.Closed,
            ChangedByUserId = CurrentUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Note = paymentNote ?? "Payment received; job closed."
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

    private static JobDetailResponse ToDetail(ServiceJob job)
    {
        var purchasesTotal = job.PurchaseLines.Sum(p => p.Quantity * p.UnitPrice);
        return new(
            job.Id,
            job.JobNumber,
            job.Status,
            job.JobType,
            job.JobTypeOtherDescription,
            JobTypeDisplay.Label(job.JobType, job.JobTypeOtherDescription),
            job.VehicleId,
            job.Vehicle.PlateNumber,
            job.Vehicle.CustomerId,
            job.Vehicle.Customer.FullName,
            job.OdometerReading,
            job.Notes,
            job.CreatedAtUtc,
            job.CompletedAtUtc,
            job.ClosedAtUtc,
            purchasesTotal,
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
}
