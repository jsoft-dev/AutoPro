namespace AutoPro.Application.Contracts.Vehicles;

public record CreateVehicleRequest(
    string PlatePrefix,
    string PlateSuffix,
    string? Vin,
    string? Make,
    string? Model,
    int? Year);
