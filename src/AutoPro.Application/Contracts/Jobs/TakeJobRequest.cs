namespace AutoPro.Application.Contracts.Jobs;

public record TakeJobRequest(Guid VehicleId, int? OdometerReading, string? Notes);
