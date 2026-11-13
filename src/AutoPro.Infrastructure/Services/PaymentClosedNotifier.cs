using System.Globalization;
using AutoPro.Application.Email;
using AutoPro.Application.Messaging;
using AutoPro.Application.Options;
using AutoPro.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPro.Infrastructure.Services;

public sealed class PaymentClosedNotifier(
    IEmailSender email,
    IOptions<EmailOptions> options,
    ILogger<PaymentClosedNotifier> logger) : IPaymentClosedNotifier
{
    public async Task NotifyPaymentReceivedAsync(ServiceJob job, decimal amountPaidLkr, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!opts.Enabled)
            return;

        var customer = job.Vehicle?.Customer;
        if (customer is null)
        {
            logger.LogWarning("Skipping payment email: job {JobId} has no customer.", job.Id);
            return;
        }

        var toEmail = customer.Email?.Trim();
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
        {
            logger.LogWarning(
                "Skipping payment email: no valid email for customer {CustomerId} on job {JobId}.",
                customer.Id,
                job.Id);
            return;
        }

        var amountText = amountPaidLkr.ToString("N2", CultureInfo.InvariantCulture);
        var name = string.IsNullOrWhiteSpace(customer.FullName) ? "Customer" : customer.FullName.Trim();
        var plate = job.Vehicle?.PlateNumber?.Trim() ?? "—";

        await email.SendPaymentThankYouAsync(toEmail, name, amountText, job.JobNumber, plate, cancellationToken);
    }
}
