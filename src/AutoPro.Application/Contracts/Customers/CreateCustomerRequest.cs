namespace AutoPro.Application.Contracts.Customers;

public record CreateCustomerRequest(string FullName, string Phone, string? Email);
