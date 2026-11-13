using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.BusinessSettings;

public record JobTypePriceResponse(
    JobType JobType,
    string JobTypeLabel,
    decimal ServiceChargeLkr,
    DateTime UpdatedAtUtc);

public record UpdateJobTypePricesRequest(
    IReadOnlyList<JobTypePriceUpdate> Prices);

public record JobTypePriceUpdate(
    JobType JobType,
    decimal ServiceChargeLkr);
