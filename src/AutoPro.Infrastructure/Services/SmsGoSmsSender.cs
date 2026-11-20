using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions SendPayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task SendPaymentThankYouAsync(
        string toE164SlMobile,
        string customerName,
        string amountFormatted,
        string jobNumber,
        string plateNumber,
        CancellationToken cancellationToken = default)
    {
        var bodyText =
            $"Thank you {customerName}! We received your payment of Rs. {amountFormatted} for vehicle {plateNumber} (job {jobNumber}). We appreciate your business — please visit AutoPro again!";

        return SendSingleAsync(
            toE164SlMobile,
            bodyText,
            options.Value.CampaignName,
            "payment thank-you",
            jobNumber,
            cancellationToken);
    }

    public Task SendCustomerRegistrationWelcomeAsync(
        string toE164SlMobile,
        string customerName,
        CancellationToken cancellationToken = default)
    {
        var bodyText =
            $"Welcome {customerName}! You are registered with AutoPro. We will send service updates to this number.";

        var campaign = $"{options.Value.CampaignName}-Registration";
        return SendSingleAsync(
            toE164SlMobile,
            bodyText,
            campaign,
            "registration welcome",
            customerName,
            cancellationToken);
    }

    private async Task SendSingleAsync(
        string toE164SlMobile,
        string bodyText,
        string campaignName,
        string occasionLabel,
        string logContext,
        CancellationToken cancellationToken)
    {
        var opts = options.Value;
        var apiKey = opts.ApiKey!.Trim();
        var mask = string.IsNullOrWhiteSpace(opts.Mask) ? null : opts.Mask.Trim();
        var requestUri = http.BaseAddress is null
            ? "(no HttpClient BaseAddress)"
            : new Uri(http.BaseAddress, "api/v1/sms/send").ToString();

        logger.LogInformation(
            "SMSGo dispatch: occasion={Occasion}, context={Context}, to={To}, mask={Mask}, campaign={Campaign}, endpoint={Endpoint}, apiKeyFingerprint={ApiKeyFingerprint}, messageChars={MessageChars}",
            occasionLabel,
            logContext,
            toE164SlMobile,
            mask ?? "(default)",
            campaignName,
            requestUri,
            SmsConfigDiagnostics.FingerprintApiKey(apiKey),
            bodyText.Length);

        var payload = new SmsGoSendRequest(toE164SlMobile, bodyText, mask, campaignName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/sms/send");
        request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
        request.Content = JsonContent.Create(payload, options: SendPayloadJsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "SMSGo HTTP request failed before response: occasion={Occasion}, to={To}, endpoint={Endpoint}",
                occasionLabel,
                toE164SlMobile,
                requestUri);
            throw;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        SmsGoSendResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<SmsGoSendResponse>(responseBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "SMSGo response JSON parse failed: occasion={Occasion}, to={To}, HTTP {Status}, body={Body}",
                occasionLabel,
                toE164SlMobile,
                (int)response.StatusCode,
                TruncateForLog(responseBody));
            throw new InvalidOperationException($"SMSGo send failed: invalid JSON response (HTTP {(int)response.StatusCode}).");
        }

        if (!response.IsSuccessStatusCode || parsed is null || !parsed.Success)
        {
            var detail = parsed?.Error ?? response.ReasonPhrase ?? "Unknown SMSGo error";
            logger.LogError(
                "SMSGo send failed: occasion={Occasion}, context={Context}, to={To}, HTTP {Status}, error={Error}, environment={Environment}, successFlag={SuccessFlag}, body={Body}",
                occasionLabel,
                logContext,
                toE164SlMobile,
                (int)response.StatusCode,
                detail,
                parsed?.Environment ?? "(n/a)",
                parsed?.Success.ToString() ?? "(n/a)",
                TruncateForLog(responseBody));
            throw new InvalidOperationException($"SMSGo send failed: {detail}");
        }

        logger.LogInformation(
            "SMSGo send succeeded: occasion={Occasion}, context={Context}, to={To}, messageId={MessageId}, status={Status}, environment={Environment}, cost={Cost}, body={Body}",
            occasionLabel,
            logContext,
            toE164SlMobile,
            parsed.Data?.Id,
            parsed.Data?.Status,
            parsed.Environment ?? "(n/a)",
            parsed.Data?.Cost,
            TruncateForLog(responseBody));
    }

    private static string TruncateForLog(string? text, int max = 500) =>
        string.IsNullOrEmpty(text) ? "(empty)" : text.Length <= max ? text : text[..max] + "…";

    private sealed record SmsGoSendRequest(
        string To,
        string Message,
        string? Mask,
        string CampaignName);

    private sealed class SmsGoSendResponse
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? Environment { get; init; }
        public SmsGoSendData? Data { get; init; }
    }

    private sealed class SmsGoSendData
    {
        public string? Id { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("cost")]
        public decimal? Cost { get; init; }
    }
}