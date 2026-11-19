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
}