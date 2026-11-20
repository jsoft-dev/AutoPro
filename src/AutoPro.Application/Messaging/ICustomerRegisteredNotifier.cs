using AutoPro.Domain.Entities;

namespace AutoPro.Application.Messaging;

public interface ICustomerRegisteredNotifier
{
    Task NotifyCustomerRegisteredAsync(Customer customer, CancellationToken cancellationToken = default);
}