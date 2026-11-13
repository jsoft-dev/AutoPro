namespace AutoPro.Application.Contracts.Vehicles;

public record VehicleJobHistoryPurchaseSummary(
    string Description,
    decimal Quantity,
    string? InventoryPartNumber,
    string? VendorName);
