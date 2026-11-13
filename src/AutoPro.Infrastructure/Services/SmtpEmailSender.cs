using AutoPro.Application.Email;
using AutoPro.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutoPro.Infrastructure.Services;

public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendPaymentThankYouAsync(
        string toEmail,
        string customerName,
        string amountFormatted,
        string jobNumber,
        string plateNumber,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var host = opts.SmtpHost!.Trim();
        var subject = $"Thank you for your payment — AutoPro ({jobNumber})";
        var bodyText =
            $"""
            Dear {customerName},

            Thank you for your payment of Rs. {amountFormatted} for vehicle {plateNumber} (job {jobNumber}).

            We appreciate your business — please visit AutoPro again!

            — AutoPro
            """;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(opts.FromName, opts.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = bodyText };

        using var client = new SmtpClient();
        var secure = opts.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(host, opts.SmtpPort, secure, cancellationToken);

        if (!string.IsNullOrWhiteSpace(opts.SmtpUser))
        {
            await client.AuthenticateAsync(opts.SmtpUser, opts.SmtpPassword ?? "", cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation(
            "Payment thank-you email sent to {ToEmail} for job {JobNumber}.",
            toEmail,
            jobNumber);
    }
}
