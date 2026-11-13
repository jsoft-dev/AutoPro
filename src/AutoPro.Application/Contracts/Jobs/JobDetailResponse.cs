using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Jobs;

public record JobListItemResponse(
    Guid Id,
    string JobNumber,
    JobStatus Status,
    JobType JobType,
    string? JobTypeOtherDescription,
    string JobTypeLabel,
    string? PlateNumber,
    string CustomerName,
    int? OdometerReading,
    DateTime CreatedAtUtc);

public record JobPurchaseLineResponse(
    Guid Id,
    PurchaseSource Source,
    Guid? InventoryItemId,
    string? InventoryPartNumber,
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

public record JobInvoiceLineItemResponse(
    Guid Id,
    InvoiceLineItemKind Kind,
    LineItemSource? Source,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string? VendorName,
    string? InventoryPartNumber);

public record JobInvoiceResponse(
    Guid Id,
    DateTime FinalizedAtUtc,
    decimal GrandTotal,
    IReadOnlyList<JobInvoiceLineItemResponse> LineItems);

public record JobDetailResponse(
    Guid Id,
    string JobNumber,
    JobStatus Status,
    JobType JobType,
    string? JobTypeOtherDescription,
    string JobTypeLabel,
    Guid? VehicleId,
    string? PlateNumber,
    Guid? CustomerId,
    string CustomerName,
    string? StoreCustomerName,
    int? OdometerReading,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ClosedAtUtc,
    decimal PurchasesTotal,
    decimal? CustomServiceChargeLkr,
    decimal JobTypeServiceChargeLkr,
    IReadOnlyList<JobPurchaseLineResponse> Purchases,
    JobInvoiceResponse? Invoice,
    IReadOnlyList<JobStatusHistoryResponse> StatusHistory,
  int? VehicleLastRecordedOdometerKm);
