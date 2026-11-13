namespace AutoPro.Application.Contracts.Customers;

public record CustomerAdminVehicleSummary(
    Guid Id,
    string PlateNumber,
    string? Make,
    string? Model,
    int? Year,
    DateTime CreatedAtUtc);
