using AutoPro.Application.Options;
using AutoPro.Application.Sms;
using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace AutoPro.Application.Tests;

public class CustomerRegisteredNotifierTests
{
    private static Customer SampleCustomer(string phone = "0771234567") =>
        new()
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Doe",
            Phone = phone,
            CreatedAtUtc = DateTime.UtcNow,
        };

    [Fact]
    public async Task When_sms_disabled_then_sender_is_not_called()
    {
        var sms = new Mock<ISmsSender>();
        var notifier = CreateNotifier(sms.Object, enabled: false);

        await notifier.NotifyCustomerRegisteredAsync(SampleCustomer());

        sms.Verify(
            s => s.SendCustomerRegistrationWelcomeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task When_phone_invalid_then_sender_is_not_called()
    {
        var sms = new Mock<ISmsSender>();
        var notifier = CreateNotifier(sms.Object, enabled: true);

        await notifier.NotifyCustomerRegisteredAsync(SampleCustomer(phone: "not-a-phone"));

        sms.Verify(
            s => s.SendCustomerRegistrationWelcomeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task When_sms_enabled_and_valid_mobile_then_sends_welcome_sms()
    {
        var sms = new Mock<ISmsSender>();
        var notifier = CreateNotifier(sms.Object, enabled: true);
        var customer = SampleCustomer();

        await notifier.NotifyCustomerRegisteredAsync(customer);

        sms.Verify(
            s => s.SendCustomerRegistrationWelcomeAsync(
                "94771234567",
                "Jane Doe",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static CustomerRegisteredNotifier CreateNotifier(ISmsSender sms, bool enabled)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new SmsOptions { Enabled = enabled, Provider = "Log" });
        return new CustomerRegisteredNotifier(sms, options);
    }
}