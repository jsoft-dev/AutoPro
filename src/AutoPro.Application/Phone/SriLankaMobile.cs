namespace AutoPro.Application.Phone;

/// <summary>Normalizes Sri Lankan mobile numbers to SMSGo format (e.g. <c>94771234567</c>).</summary>
public static class SriLankaMobile
{
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        if (digits.StartsWith('0') && digits.Length == 10)
            digits = "94" + digits[1..];

        if (digits.StartsWith("94") && digits.Length is >= 11 and <= 12)
            return digits;

        return null;
    }
}