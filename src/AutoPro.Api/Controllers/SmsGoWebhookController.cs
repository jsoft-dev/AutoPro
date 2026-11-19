using System.Security.Cryptography;
using System.Text;
using AutoPro.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AutoPro.Api.Controllers;

/// <summary>Delivery status callbacks from SMSGo.lk (configure URL in SMSGo dashboard).</summary>
[ApiController]
[Route("api/v1/webhooks/smsgo")]
[AllowAnonymous]
public sealed class SmsGoWebhookController(
    IOptions<SmsOptions> smsOptions,
    ILogger<SmsGoWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var opts = smsOptions.Value;
        if (string.Equals(opts.Provider, "SmsGo", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(opts.ApiKey) &&
            Request.Headers.TryGetValue("X-SMSGo-Signature", out var signatureHeader))
        {
            var expected = ComputeHmacSha256Hex(body, opts.ApiKey.Trim());
            var provided = signatureHeader.ToString().Trim();
            if (provided.Length != expected.Length ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(provided)))
            {
                logger.LogWarning("SMSGo webhook rejected: invalid X-SMSGo-Signature.");
                return Unauthorized();
            }
        }

        logger.LogInformation("SMSGo webhook payload: {Body}", body);
        return Ok();
    }

    private static string ComputeHmacSha256Hex(string payload, string apiKey)
    {
        var key = Encoding.UTF8.GetBytes(apiKey);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}