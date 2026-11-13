namespace AutoPro.Domain.Services;

public static class PaymentMethodLabels
{
    public const string Cash = "Cash";
    public const string Card = "Card";
    public const string BankTransfer = "Bank Transfer";

    public static readonly string[] All = [Cash, Card, BankTransfer];

    public static bool IsAllowed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        return All.Any(m => string.Equals(m, trimmed, StringComparison.Ordinal));
    }
}
