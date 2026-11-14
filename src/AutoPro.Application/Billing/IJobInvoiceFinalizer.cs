namespace AutoPro.Application.Billing;

public interface IJobInvoiceFinalizer
{
    /// <summary>Creates an empty invoice when the job has none yet.</summary>
    Task EnsureForJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default);

    /// <summary>Adds a purchase line to the job invoice (idempotent per purchase id).</summary>
    Task AddPurchaseLineAsync(Guid serviceJobId, Guid purchaseLineId, CancellationToken cancellationToken = default);

    /// <summary>Adds the service charge line when the CR completes the job (does not replace existing lines).</summary>
    Task EnsureServiceChargeOnCompletionAsync(Guid serviceJobId, CancellationToken cancellationToken = default);

    /// <summary>Sets invoice finalized timestamp when payment is received.</summary>
    Task StampFinalizedAtCloseAsync(Guid serviceJobId, CancellationToken cancellationToken = default);

    /// <summary>Ensures invoice exists and all purchases (and service charge when applicable) are reflected.</summary>
    Task FinalizeForJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default);
}