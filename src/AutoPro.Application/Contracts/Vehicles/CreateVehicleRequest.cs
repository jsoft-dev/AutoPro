namespace AutoPro.Application.Contracts.Vehicles;

public record CreateVehicleRequest(
    string PlateNumber,
    string? Vin,
    string? Make,
    string? Model,
    int? Year);
