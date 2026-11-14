using AutoPro.Application.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoPro.Infrastructure.Persistence;

/// <summary>
/// One-time repair for jobs created before live invoices existed.
/// </summary>
public static class JobInvoiceBackfill
{
    public static async Task RunAsync(
        AutoProDbContext db,
        IJobInvoiceFinalizer invoiceFinalizer,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var jobIds = await db.ServiceJobs
            .AsNoTracking()
            .Where(j => j.Invoice == null)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        if (jobIds.Count == 0) return;

        logger.LogInformation("Backfilling {Count} job invoice(s).", jobIds.Count);

        foreach (var jobId in jobIds)
        {
            try
            {
                await invoiceFinalizer.FinalizeForJobAsync(jobId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not backfill invoice for job {JobId}.", jobId);
            }
        }
    }
}