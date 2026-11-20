namespace AutoPro.Application.Sms;

public interface ISmsSender
{
    Task SendPaymentThankYouAsync(
        string toE164SlMobile,
        string customerName,
        string amountFormatted,
        string jobNumber,
        string plateNumber,
        CancellationToken cancellationToken = default);

    Task SendCustomerRegistrationWelcomeAsync(
        string toE164SlMobile,
        string customerName,
        CancellationToken cancellationToken = default);
}