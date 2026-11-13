namespace AutoPro.Application.Billing;

public interface IJobInvoiceFinalizer
{
    /// <summary>
    /// Builds or replaces the job invoice from current purchases and job-type service charge (Option A: at completion).
    /// </summary>
    Task FinalizeForJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default);
}
