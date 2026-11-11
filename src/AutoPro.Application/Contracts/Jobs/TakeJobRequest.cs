using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Jobs;

public record TakeJobRequest(
    Guid VehicleId,
    JobType JobType,
    string? JobTypeOtherDescription,
    int? OdometerReading,
    string? Notes);
