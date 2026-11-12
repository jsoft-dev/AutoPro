namespace AutoPro.Application.Contracts.Vehicles;

public record VehicleQrLookupResponse(
    Guid VehicleId,
    string PlatePrefix,
    string PlateSuffix,
    string PlateNumber,
    string? Make,
    string? Model,
    int? Year,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone);
