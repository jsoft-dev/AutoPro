using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Jobs;

public record UpdateJobRequest(
    JobType JobType,
    string? JobTypeOtherDescription,
    decimal? CustomServiceChargeLkr,
    int? OdometerReading);
