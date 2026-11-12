namespace AutoPro.Application.Contracts.Vehicles;

public record VehicleAdminListItemResponse(
    Guid Id,
    string PlatePrefix,
    string PlateSuffix,
    string PlateNumber,
    string? Make,
    string? Model,
    int? Year,
    string CustomerName,
    string CustomerPhone,
    string QrPayload,
    DateTime CreatedAtUtc);
