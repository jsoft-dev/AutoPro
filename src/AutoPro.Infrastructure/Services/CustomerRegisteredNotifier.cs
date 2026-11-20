using AutoPro.Application.Messaging;
using AutoPro.Application.Options;
using AutoPro.Application.Phone;
using AutoPro.Application.Sms;
using AutoPro.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AutoPro.Infrastructure.Services;

public sealed class CustomerRegisteredNotifier(
    ISmsSender sms,
    IOptions<SmsOptions> smsOptions) : ICustomerRegisteredNotifier
{
    public async Task NotifyCustomerRegisteredAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (!smsOptions.Value.Enabled)
            return;

        var name = string.IsNullOrWhiteSpace(customer.FullName) ? "Customer" : customer.FullName.Trim();
        var mobile = SriLankaMobile.Normalize(customer.Phone);
        if (mobile is null)
            return;

        await sms.SendCustomerRegistrationWelcomeAsync(mobile, name, cancellationToken);
    }
}