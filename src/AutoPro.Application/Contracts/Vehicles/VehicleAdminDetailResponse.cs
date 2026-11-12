namespace AutoPro.Application.Contracts.Vehicles;

public record VehicleAdminDetailResponse(
    Guid Id,
    string PlateNumber,
    string? Vin,
    string? Make,
    string? Model,
    int? Year,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string QrPayload,
    DateTime CreatedAtUtc);
