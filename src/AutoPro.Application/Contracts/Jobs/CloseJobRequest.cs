namespace AutoPro.Application.Contracts.Jobs;

/// <param name="PaymentNote">Payment method: Cash, Card, or Bank Transfer.</param>
public record CloseJobRequest(string? PaymentNote);
