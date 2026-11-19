namespace AutoPro.Application.Contracts.Inventory;

public record InventoryImportRowError(int RowNumber, string Message);

public record InventoryImportResultResponse(
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    IReadOnlyList<InventoryImportRowError> Errors);