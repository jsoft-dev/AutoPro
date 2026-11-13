namespace AutoPro.Application.Contracts.Customers;

public record UpdateCustomerRequest(string FullName, string Phone, string? Email);
