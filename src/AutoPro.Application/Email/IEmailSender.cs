namespace AutoPro.Application.Email;

public interface IEmailSender
{
    Task SendPaymentThankYouAsync(
        string toEmail,
        string customerName,
        string amountFormatted,
        string jobNumber,
        string plateNumber,
        CancellationToken cancellationToken = default);
}
