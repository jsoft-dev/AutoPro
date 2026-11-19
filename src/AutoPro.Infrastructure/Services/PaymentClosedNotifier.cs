using System.Globalization;
using AutoPro.Application.Email;
using AutoPro.Application.Messaging;
using AutoPro.Application.Options;
using AutoPro.Application.Phone;
using AutoPro.Application.Sms;
using AutoPro.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPro.Infrastructure.Services;

public sealed class PaymentClosedNotifier(
    IEmailSender email,
    ISmsSender sms,
    IOptions<EmailOptions> emailOptions,
    IOptions<SmsOptions> smsOptions,
    ILogger<PaymentClosedNotifier> logger) : IPaymentClosedNotifier
{
    public async Task NotifyPaymentReceivedAsync(ServiceJob job, decimal amountPaidLkr, CancellationToken cancellationToken = default)
    {
        var customer = job.Vehicle?.Customer;
        if (customer is null)
        {
            logger.LogWarning("Skipping payment notifications: job {JobId} has no customer.", job.Id);
            return;
        }

        var amountText = amountPaidLkr.ToString("N2", CultureInfo.InvariantCulture);
        var name = string.IsNullOrWhiteSpace(customer.FullName) ? "Customer" : customer.FullName.Trim();
        var plate = job.Vehicle?.PlateNumber?.Trim() ?? "—";

        var emailOpts = emailOptions.Value;
        if (emailOpts.Enabled)
        {
            var toEmail = customer.Email?.Trim();
            if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
            {
                logger.LogWarning(
                    "Skipping payment email: no valid email for customer {CustomerId} on job {JobId}.",
                    customer.Id,
                    job.Id);
            }
            else
            {
                await email.SendPaymentThankYouAsync(toEmail, name, amountText, job.JobNumber, plate, cancellationToken);
            }
        }

        var smsOpts = smsOptions.Value;
        if (!smsOpts.Enabled)
            return;

        var mobile = SriLankaMobile.Normalize(customer.Phone);
        if (mobile is null)
        {
            logger.LogWarning(
                "Skipping payment SMS: phone {Phone} is not a valid Sri Lankan mobile for customer {CustomerId} on job {JobId}.",
                customer.Phone,
                customer.Id,
                job.Id);
            return;
        }

        await sms.SendPaymentThankYouAsync(mobile, name, amountText, job.JobNumber, plate, cancellationToken);
    }
}