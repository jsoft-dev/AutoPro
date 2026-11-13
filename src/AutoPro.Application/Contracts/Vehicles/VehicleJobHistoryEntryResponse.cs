namespace AutoPro.Application.Contracts.Vehicles;

public record VehicleJobHistoryEntryResponse(
    Guid Id,
    string JobNumber,
    string JobTypeLabel,
    string Status,
    int? OdometerReading,
    string? Notes,
    IReadOnlyList<string> StatusNotes,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<VehicleJobHistoryPurchaseSummary> Purchases);
