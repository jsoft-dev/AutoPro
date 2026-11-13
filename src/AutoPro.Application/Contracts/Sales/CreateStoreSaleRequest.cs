namespace AutoPro.Application.Contracts.Sales;

public record CreateStoreSaleRequest(
    string? StoreCustomerName,
    string? Notes);

public record CompleteStoreSaleRequest(string? Note);

public record UpdateStoreSaleRequest(
    string? StoreCustomerName,
    string? Notes);
