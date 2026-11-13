using AutoPro.Domain.Entities;

namespace AutoPro.Application.Messaging;

public interface IPaymentClosedNotifier
{
    Task NotifyPaymentReceivedAsync(ServiceJob job, decimal amountPaidLkr, CancellationToken cancellationToken = default);
}
