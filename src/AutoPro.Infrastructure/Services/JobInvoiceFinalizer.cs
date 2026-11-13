using AutoPro.Application.Billing;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public sealed class JobInvoiceFinalizer(AutoProDbContext db) : IJobInvoiceFinalizer
{
    public async Task FinalizeForJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await db.ServiceJobs
            .Include(j => j.PurchaseLines).ThenInclude(p => p.InventoryItem)
            .Include(j => j.Invoice!).ThenInclude(i => i.LineItems)
            .FirstOrDefaultAsync(j => j.Id == serviceJobId, cancellationToken);

        if (job is null)
            throw new InvalidOperationException($"Service job {serviceJobId} was not found.");

        if (job.Invoice is not null)
        {
            db.JobInvoiceLineItems.RemoveRange(job.Invoice.LineItems);
            db.JobInvoices.Remove(job.Invoice);
            job.Invoice = null;
        }

        var finalizedAt = DateTime.UtcNow;

        var invoice = new JobInvoice
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            FinalizedAtUtc = finalizedAt,
        };

        var lineItems = new List<JobInvoiceLineItem>();
        var order = 0;

        if (job.JobType != JobType.StoreSale)
        {
            var configured = await JobTypeServicePriceLookup.GetConfiguredChargeAsync(db, job.JobType, cancellationToken);
            var serviceCharge = JobServiceChargeResolver.EffectiveCharge(job, configured);
            var jobTypeLabel = JobTypeDisplay.Label(job.JobType, job.JobTypeOtherDescription);
            lineItems.Add(new JobInvoiceLineItem
            {
                Id = Guid.NewGuid(),
                JobInvoice = invoice,
                Kind = InvoiceLineItemKind.ServiceCharge,
                Source = null,
                Description = $"Service charge — {jobTypeLabel}",
                Quantity = 1,
                UnitPrice = serviceCharge,
                SortOrder = order++,
            });
        }
        foreach (var purchase in job.PurchaseLines.OrderBy(p => p.CreatedAtUtc))
        {
            lineItems.Add(new JobInvoiceLineItem
            {
                Id = Guid.NewGuid(),
                JobInvoice = invoice,
                Kind = InvoiceLineItemKind.Purchase,
                Source = PurchaseSourceMapping.ToLineItemSource(purchase.Source),
                JobPurchaseLineId = purchase.Id,
                Description = purchase.Description,
                Quantity = purchase.Quantity,
                UnitPrice = purchase.UnitPrice,
                SortOrder = order++,
            });
        }

        invoice.LineItems = lineItems;
        job.Invoice = invoice;
        db.JobInvoices.Add(invoice);
    }
}
