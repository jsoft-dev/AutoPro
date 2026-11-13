using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Jobs;

public record TakeJobRequest(
    Guid VehicleId,
    JobType JobType,
    string? JobTypeOtherDescription,
    decimal? CustomServiceChargeLkr,
    int? OdometerReading,
    string? Notes);
