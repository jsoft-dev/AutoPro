using System.Text.RegularExpressions;

namespace AutoPro.Application.Vehicles;

public static partial class PlateNumber
{
    private static readonly Regex LegacyPlatePattern = LegacyRegex();

    public static bool TryNormalizeParts(
        string? prefix,
        string? suffix,
        out string normalizedPrefix,
        out string normalizedSuffix,
        out string? error)
    {
        normalizedPrefix = string.Empty;
        normalizedSuffix = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(suffix))
        {
            error = "Plate prefix and suffix are required.";
            return false;
        }

        normalizedPrefix = prefix.Trim().ToUpperInvariant();
        normalizedSuffix = suffix.Trim();

        if (!Regex.IsMatch(normalizedPrefix, "^[A-Z]{1,7}$"))
        {
            error = "Plate prefix must be 1–7 letters (e.g. CAP, VO).";
            return false;
        }

        if (!Regex.IsMatch(normalizedSuffix, "^[0-9]{1,7}$"))
        {
            error = "Plate suffix must be digits only (e.g. 1234).";
            return false;
        }

        return true;
    }

    public static bool TryParseCombined(
        string? plate,
        out string normalizedPrefix,
        out string normalizedSuffix,
        out string? error)
    {
        normalizedPrefix = string.Empty;
        normalizedSuffix = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(plate))
        {
            error = "Registration number is required.";
            return false;
        }

        var compact = plate.Trim().ToUpperInvariant().Replace(" ", string.Empty);
        var match = LegacyPlatePattern.Match(compact);
        if (!match.Success)
        {
            error = "Registration number must look like CAP-1234.";
            return false;
        }

        normalizedPrefix = match.Groups[1].Value;
        normalizedSuffix = match.Groups[2].Value;
        return true;
    }

    public static string Format(string prefix, string suffix) =>
        $"{prefix.ToUpperInvariant()}-{suffix.Trim()}";

    public static bool TryResolve(
        string? plate,
        string? prefix,
        string? suffix,
        out string normalizedPrefix,
        out string normalizedSuffix,
        out string? error)
    {
        if (!string.IsNullOrWhiteSpace(prefix) && !string.IsNullOrWhiteSpace(suffix))
            return TryNormalizeParts(prefix, suffix, out normalizedPrefix, out normalizedSuffix, out error);

        if (!string.IsNullOrWhiteSpace(plate))
            return TryParseCombined(plate, out normalizedPrefix, out normalizedSuffix, out error);

        normalizedPrefix = string.Empty;
        normalizedSuffix = string.Empty;
        error = "Registration number is required.";
        return false;
    }

    public static (string Prefix, string Suffix)? TrySplitExisting(string plateNumber)
    {
        if (TryParseCombined(plateNumber, out var prefix, out var suffix, out _))
            return (prefix, suffix);
        return null;
    }

    [GeneratedRegex(@"^([A-Z]{1,7})-([0-9]{1,7})$", RegexOptions.CultureInvariant)]
    private static partial Regex LegacyRegex();
}
