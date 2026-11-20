using AutoPro.Application.Messaging;
using AutoPro.Application.Options;
using AutoPro.Application.Phone;
using AutoPro.Application.Sms;
using AutoPro.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPro.Infrastructure.Services;

public sealed class CustomerRegisteredNotifier(
    ISmsSender sms,
    IOptions<SmsOptions> smsOptions,
    ILogger<CustomerRegisteredNotifier> logger) : ICustomerRegisteredNotifier
{
    public async Task NotifyCustomerRegisteredAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var smsOpts = smsOptions.Value;
        logger.LogInformation(
            "Registration SMS check: customerId={CustomerId}, rawPhone={RawPhone}, smsEnabled={Enabled}, provider={Provider}, senderImpl={SenderImpl}, apiKeyFingerprint={ApiKeyFingerprint}",
            customer.Id,
            customer.Phone,
            smsOpts.Enabled,
            smsOpts.Provider,
            SmsConfigDiagnostics.SmsImplType(smsOpts),
            SmsConfigDiagnostics.FingerprintApiKey(smsOpts.ApiKey));

        if (!smsOpts.Enabled)
        {
            logger.LogInformation("Registration SMS skipped: Sms:Enabled is false.");
            return;
        }

        var name = string.IsNullOrWhiteSpace(customer.FullName) ? "Customer" : customer.FullName.Trim();
        var mobile = SriLankaMobile.Normalize(customer.Phone);
        if (mobile is null)
        {
            logger.LogWarning(
                "Registration SMS skipped: phone {Phone} is not a valid Sri Lankan mobile for customer {CustomerId}.",
                customer.Phone,
                customer.Id);
            return;
        }

        logger.LogInformation(
            "Registration SMS sending: customerId={CustomerId}, normalizedTo={To}, customerName={Name}",
            customer.Id,
            mobile,
            name);

        await sms.SendCustomerRegistrationWelcomeAsync(mobile, name, cancellationToken);

        logger.LogInformation(
            "Registration SMS completed without exception: customerId={CustomerId}, to={To}",
            customer.Id,
            mobile);
    }
}