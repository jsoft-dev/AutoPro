using AutoPro.Application.Contracts.Vehicles;
using AutoPro.Application.Vehicles;
using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
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

        return Ok(await ToLookupAsync(vehicle, ct));
    }

    [HttpGet("by-plate")]
    public async Task<ActionResult<VehicleQrLookupResponse>> GetByPlate(
        [FromQuery] string? plate,
        [FromQuery] string? prefix,
        [FromQuery] string? suffix,
        CancellationToken ct)
    {
        if (!PlateNumber.TryResolve(plate, prefix, suffix, out var platePrefix, out var plateSuffix, out var error))
            return BadRequest(new { message = error });

        var formatted = PlateNumber.Format(platePrefix, plateSuffix);
        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(
                v => v.PlatePrefix == platePrefix && v.PlateSuffix == plateSuffix
                     || v.PlateNumber == formatted,
                ct);

        if (vehicle is null)
            return NotFound(new { message = "No vehicle with that registration number." });

        return Ok(await ToLookupAsync(vehicle, ct));
    }

    private async Task<VehicleQrLookupResponse> ToLookupAsync(Vehicle vehicle, CancellationToken ct)
    {
        var lastOdometer = await VehicleOdometerLookup.GetLastRecordedAsync(
            db,
            vehicle.Id,
            excludeJobId: null,
            ct);

        return new(
            vehicle.Id,
            vehicle.PlatePrefix,
            vehicle.PlateSuffix,
            vehicle.PlateNumber,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.CustomerId,
            vehicle.Customer.FullName,
            vehicle.Customer.Phone,
            lastOdometer);
    }

    [HttpPost("~/api/v1/customers/{customerId:guid}/vehicles")]
    public async Task<ActionResult<VehicleCreatedResponse>> CreateForCustomer(
        Guid customerId,
        [FromBody] CreateVehicleRequest request,
        CancellationToken ct)
    {
        var exists = await db.Customers.AnyAsync(c => c.Id == customerId, ct);
        if (!exists) return NotFound(new { message = "Customer not found." });

        if (!PlateNumber.TryNormalizeParts(
                request.PlatePrefix,
                request.PlateSuffix,
                out var platePrefix,
                out var plateSuffix,
                out var error))
            return BadRequest(new { message = error });

        var plateNumber = PlateNumber.Format(platePrefix, plateSuffix);

        var duplicate = await db.Vehicles.AnyAsync(
            v => v.PlatePrefix == platePrefix && v.PlateSuffix == plateSuffix, ct);
        if (duplicate)
            return Conflict(new { message = $"Vehicle {plateNumber} is already registered." });

        var qrToken = Guid.NewGuid().ToString("N");
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            QrToken = qrToken,
            PlatePrefix = platePrefix,
            PlateSuffix = plateSuffix,
            PlateNumber = plateNumber,
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
