using AutoPro.Application.Email;
using Microsoft.Extensions.Logging;

namespace AutoPro.Infrastructure.Services;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendPaymentThankYouAsync(
        string toEmail,
        string customerName,
        string amountFormatted,
        string jobNumber,
        string plateNumber,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            """
            Email (log-only) → {ToEmail}
            Subject: Thank you for your payment — AutoPro ({JobNumber})
            Body: Thank you {CustomerName} for your payment of Rs. {Amount} for vehicle {PlateNumber} (job {JobNumber}). We appreciate your business — please visit AutoPro again!
            """,
            toEmail,
            jobNumber,
            customerName,
            amountFormatted,
            plateNumber,
            jobNumber);

        return Task.CompletedTask;
    }
}
