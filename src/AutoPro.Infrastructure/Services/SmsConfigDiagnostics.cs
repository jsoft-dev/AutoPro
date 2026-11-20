using AutoPro.Application.Options;

namespace AutoPro.Infrastructure.Services;

public static class SmsConfigDiagnostics
{
    public static string FingerprintApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return "(missing)";

        var trimmed = apiKey.Trim();
        if (trimmed.Length <= 12)
            return "(too short; check Sms:ApiKey)";

        if (trimmed.Contains("REPLACE", StringComparison.OrdinalIgnoreCase))
            return "(placeholder value — replace Sms:ApiKey)";

        return $"{trimmed[..7]}…{trimmed[^4..]}";
    }

    public static string SmsImplType(SmsOptions sms) =>
        sms.Enabled && string.Equals(sms.Provider, "SmsGo", StringComparison.OrdinalIgnoreCase)
            ? nameof(SmsGoSmsSender)
            : nameof(LoggingSmsSender);
}