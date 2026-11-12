using AutoPro.Application.Contracts.Vehicles;
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
    public async Task<ActionResult<IReadOnlyList<VehicleAdminListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 200);

        var query = db.Vehicles.AsNoTracking().Include(v => v.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var plateTerm = term.ToUpperInvariant();
            query = query.Where(v =>
                v.PlateNumber.Contains(plateTerm) ||
                EF.Functions.ILike(v.Customer.FullName, $"%{term}%") ||
                v.Customer.Phone.Contains(term));
        }

        var vehicles = await query
            .OrderByDescending(v => v.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(ct);

        return Ok(vehicles.Select(ToListItem).ToList());
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

    private static VehicleAdminListItemResponse ToListItem(Domain.Entities.Vehicle vehicle) =>
        new(
            vehicle.Id,
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
