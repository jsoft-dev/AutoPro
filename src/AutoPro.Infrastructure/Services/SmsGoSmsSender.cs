using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutoPro.Application.Options;
using AutoPro.Application.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPro.Infrastructure.Services;

public sealed class SmsGoSmsSender(
    HttpClient http,
    IOptions<SmsOptions> options,
    ILogger<SmsGoSmsSender> logger) : ISmsSender
{
    public async Task SendPaymentThankYouAsync(
        string toE164SlMobile,
        string customerName,
        string amountFormatted,
        string jobNumber,
        string plateNumber,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var apiKey = opts.ApiKey!.Trim();

        var bodyText =
            $"Thank you {customerName}! We received your payment of Rs. {amountFormatted} for vehicle {plateNumber} (job {jobNumber}). We appreciate your business — please visit AutoPro again!";

        var payload = new SmsGoSendRequest(
            toE164SlMobile,
            bodyText,
            string.IsNullOrWhiteSpace(opts.Mask) ? null : opts.Mask.Trim(),
            opts.CampaignName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/sms/send");
        request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
        request.Content = JsonContent.Create(payload);

        var response = await http.SendAsync(request, cancellationToken);
        var parsed = await response.Content.ReadFromJsonAsync<SmsGoSendResponse>(cancellationToken);

        if (!response.IsSuccessStatusCode || parsed is null || !parsed.Success)
        {
            var detail = parsed?.Error ?? response.ReasonPhrase ?? "Unknown SMSGo error";
            logger.LogError(
                "SMSGo send failed for job {JobNumber} to {To}: HTTP {Status} — {Detail}",
                jobNumber,
                toE164SlMobile,
                (int)response.StatusCode,
                detail);
            throw new InvalidOperationException($"SMSGo send failed: {detail}");
        }

        logger.LogInformation(
            "Payment thank-you SMS sent via SMSGo to {To} for job {JobNumber} (id={MessageId}, status={Status}).",
            toE164SlMobile,
            jobNumber,
            parsed.Data?.Id,
            parsed.Data?.Status);
    }

    private sealed record SmsGoSendRequest(
        string To,
        string Message,
        string? Mask,
        string CampaignName);

    private sealed class SmsGoSendResponse
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public SmsGoSendData? Data { get; init; }
    }

    private sealed class SmsGoSendData
    {
        public string? Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }
    }
}