using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.Vehicles;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/vehicles")]
public class AdminVehiclesController(AutoProDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<VehicleAdminListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Vehicles.AsNoTracking().Include(v => v.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var plateTerm = term.ToUpperInvariant();
            query = query.Where(v =>
                v.PlateNumber.Contains(plateTerm) ||
                v.PlatePrefix.Contains(plateTerm) ||
                v.PlateSuffix.Contains(term) ||
                EF.Functions.ILike(v.Customer.FullName, $"%{term}%") ||
                v.Customer.Phone.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var vehicles = await query
            .OrderByDescending(v => v.CreatedAtUtc)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = vehicles.Select(ToListItem).ToList();
        return Ok(new PagedResult<VehicleAdminListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleAdminDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (vehicle is null) return NotFound(new { message = "Vehicle not found." });

        return Ok(ToDetail(vehicle));
    }

    [HttpGet("{id:guid}/job-history")]
    public async Task<ActionResult<IReadOnlyList<VehicleJobHistoryEntryResponse>>> JobHistory(
        Guid id,
        CancellationToken ct)
    {
        var exists = await db.Vehicles.AsNoTracking().AnyAsync(v => v.Id == id, ct);
        if (!exists) return NotFound(new { message = "Vehicle not found." });

        var jobs = await db.ServiceJobs.AsNoTracking()
            .Where(j => j.VehicleId == id)
            .OrderByDescending(j => j.CreatedAtUtc)
            .ThenByDescending(j => j.Id)
            .Include(j => j.PurchaseLines)
                .ThenInclude(p => p.InventoryItem)
            .Include(j => j.StatusHistory)
            .ToListAsync(ct);

        var items = jobs.Select(j => new VehicleJobHistoryEntryResponse(
            j.Id,
            j.JobNumber,
            JobTypeDisplay.Label(j.JobType, j.JobTypeOtherDescription),
            j.Status.ToString(),
            j.OdometerReading,
            string.IsNullOrWhiteSpace(j.Notes) ? null : j.Notes.Trim(),
            j.StatusHistory
                .Where(h => !string.IsNullOrWhiteSpace(h.Note))
                .OrderBy(h => h.ChangedAtUtc)
                .Select(h => h.Note!.Trim())
                .ToList(),
            j.CreatedAtUtc,
            j.CompletedAtUtc,
            j.PurchaseLines
                .OrderBy(p => p.CreatedAtUtc)
                .Select(p => new VehicleJobHistoryPurchaseSummary(
                    p.Description,
                    p.Quantity,
                    p.InventoryItem?.PartNumber,
                    string.IsNullOrWhiteSpace(p.VendorName) ? null : p.VendorName.Trim()))
                .ToList()))
            .ToList();

        return Ok(items);
    }

    private static VehicleAdminListItemResponse ToListItem(Domain.Entities.Vehicle vehicle) =>
        new(
            vehicle.Id,
            vehicle.PlatePrefix,
            vehicle.PlateSuffix,
            vehicle.PlateNumber,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.Customer.FullName,
            vehicle.Customer.Phone,
            VehiclesController.QrPayloadPrefix + vehicle.QrToken,
            vehicle.CreatedAtUtc);

    private static VehicleAdminDetailResponse ToDetail(Domain.Entities.Vehicle vehicle) =>
        new(
            vehicle.Id,
            vehicle.PlatePrefix,
            vehicle.PlateSuffix,
            vehicle.PlateNumber,
            vehicle.Vin,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.CustomerId,
            vehicle.Customer.FullName,
            vehicle.Customer.Phone,
            VehiclesController.QrPayloadPrefix + vehicle.QrToken,
            vehicle.CreatedAtUtc);
}
