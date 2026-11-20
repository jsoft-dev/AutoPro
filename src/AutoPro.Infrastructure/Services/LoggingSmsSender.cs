using AutoPro.Application.Sms;
using Microsoft.Extensions.Logging;

namespace AutoPro.Infrastructure.Services;

public sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendPaymentThankYouAsync(
        string toE164SlMobile,
        string customerName,
        string amountFormatted,
        string jobNumber,
        string plateNumber,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            """
            SMS (log-only) → {To}
            Body: Thank you {CustomerName}! We received your payment of Rs. {Amount} for vehicle {PlateNumber} (job {JobNumber}). We appreciate your business — please visit AutoPro again!
            """,
            toE164SlMobile,
            customerName,
            amountFormatted,
            plateNumber,
            jobNumber);

        return Task.CompletedTask;
    }

    public Task SendCustomerRegistrationWelcomeAsync(
        string toE164SlMobile,
        string customerName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            """
            SMS (log-only) → {To}
            Body: Welcome {CustomerName}! You are registered with AutoPro. We will send service updates to this number.
            """,
            toE164SlMobile,
            customerName);

        return Task.CompletedTask;
    }
}