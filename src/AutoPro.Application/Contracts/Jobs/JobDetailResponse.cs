using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Jobs;

public record JobListItemResponse(
    Guid Id,
    string JobNumber,
    JobStatus Status,
    string PlateNumber,
    string CustomerName,
    int? OdometerReading,
    DateTime CreatedAtUtc);

public record JobPurchaseLineResponse(
    Guid Id,
    PurchaseSource Source,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string? VendorName,
    DateTime CreatedAtUtc);

public record JobStatusHistoryResponse(
    JobStatus FromStatus,
    JobStatus ToStatus,
    DateTime ChangedAtUtc,
    string? Note);

public record JobDetailResponse(
    Guid Id,
    string JobNumber,
    JobStatus Status,
    Guid VehicleId,
    string PlateNumber,
    Guid CustomerId,
    string CustomerName,
    int? OdometerReading,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<JobPurchaseLineResponse> Purchases,
    IReadOnlyList<JobStatusHistoryResponse> StatusHistory);
