using AutoPro.Application.Contracts.Vehicles;
using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "CustomerRepresentative")]
[Route("api/v1/vehicles")]
public class VehiclesController(AutoProDbContext db) : ControllerBase
{
  public const string QrPayloadPrefix = "autopro://v/";

    [HttpGet("by-qr/{token}")]
    public async Task<ActionResult<VehicleQrLookupResponse>> GetByQr(string token, CancellationToken ct)
    {
        token = token.Trim();
        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.QrToken == token, ct);

        if (vehicle is null)
            return NotFound(new { message = "Vehicle QR not recognized." });

        return Ok(ToLookup(vehicle));
    }

    [HttpGet("by-plate")]
    public async Task<ActionResult<VehicleQrLookupResponse>> GetByPlate(
        [FromQuery] string plate,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return BadRequest(new { message = "Registration number is required." });

        var normalized = plate.Trim().ToUpperInvariant();
        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.PlateNumber == normalized, ct);

        if (vehicle is null)
            return NotFound(new { message = "No vehicle with that registration number." });

        return Ok(ToLookup(vehicle));
    }

    private static VehicleQrLookupResponse ToLookup(Vehicle vehicle) =>
        new(
            vehicle.Id,
            vehicle.PlateNumber,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.CustomerId,
            vehicle.Customer.FullName,
            vehicle.Customer.Phone);

    [HttpPost("~/api/v1/customers/{customerId:guid}/vehicles")]
    public async Task<ActionResult<VehicleCreatedResponse>> CreateForCustomer(
        Guid customerId,
        [FromBody] CreateVehicleRequest request,
        CancellationToken ct)
    {
        var exists = await db.Customers.AnyAsync(c => c.Id == customerId, ct);
        if (!exists) return NotFound(new { message = "Customer not found." });

        var qrToken = Guid.NewGuid().ToString("N");
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            QrToken = qrToken,
            PlateNumber = request.PlateNumber.Trim().ToUpperInvariant(),
            Vin = request.Vin?.Trim(),
            Make = request.Make?.Trim(),
            Model = request.Model?.Trim(),
            Year = request.Year,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(ct);

        return Created(string.Empty, new VehicleCreatedResponse(
            vehicle.Id,
            qrToken,
            QrPayloadPrefix + qrToken));
    }
}
