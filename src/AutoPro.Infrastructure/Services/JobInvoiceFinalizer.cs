using AutoPro.Application.Billing;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public sealed class JobInvoiceFinalizer(AutoProDbContext db) : IJobInvoiceFinalizer
{
    public Task EnsureForJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default) =>
        SyncInvoiceAsync(serviceJobId, includeServiceCharge: false, cancellationToken);

    public async Task AddPurchaseLineAsync(
        Guid serviceJobId,
        Guid purchaseLineId,
        CancellationToken cancellationToken = default)
    {
        var job = await LoadTrackedJobAsync(serviceJobId, cancellationToken);
        if (job is null)
            throw new InvalidOperationException($"Service job {serviceJobId} was not found.");

        var purchase = job.PurchaseLines.FirstOrDefault(p => p.Id == purchaseLineId);
        if (purchase is null)
            throw new InvalidOperationException($"Purchase line {purchaseLineId} was not found on job {serviceJobId}.");

        EnsureInvoiceEntity(job);
        AppendPurchaseLineIfMissing(job.Invoice!, purchase);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task EnsureServiceChargeOnCompletionAsync(Guid serviceJobId, CancellationToken cancellationToken = default) =>
        SyncInvoiceAsync(serviceJobId, includeServiceCharge: true, cancellationToken);

    public async Task StampFinalizedAtCloseAsync(Guid serviceJobId, CancellationToken cancellationToken = default)
    {
        var job = await LoadTrackedJobAsync(serviceJobId, cancellationToken);
        if (job is null) return;

        EnsureInvoiceEntity(job);
        job.Invoice!.FinalizedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task FinalizeForJobAsync(Guid serviceJobId, CancellationToken cancellationToken = default) =>
        SyncInvoiceAsync(serviceJobId, includeServiceCharge: true, cancellationToken);

    private async Task SyncInvoiceAsync(
        Guid serviceJobId,
        bool includeServiceCharge,
        CancellationToken cancellationToken)
    {
        var job = await LoadTrackedJobAsync(serviceJobId, cancellationToken);
        if (job is null)
            throw new InvalidOperationException($"Service job {serviceJobId} was not found.");

        EnsureInvoiceEntity(job);

        foreach (var purchase in job.PurchaseLines.OrderBy(p => p.CreatedAtUtc))
            AppendPurchaseLineIfMissing(job.Invoice!, purchase);

        if (includeServiceCharge)
            await EnsureServiceChargeLineAsync(job, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ServiceJob?> LoadTrackedJobAsync(Guid serviceJobId, CancellationToken cancellationToken) =>
        await db.ServiceJobs
            .Include(j => j.PurchaseLines).ThenInclude(p => p.InventoryItem)
            .Include(j => j.Invoice!).ThenInclude(i => i.LineItems)
            .FirstOrDefaultAsync(j => j.Id == serviceJobId, cancellationToken);

    private void EnsureInvoiceEntity(ServiceJob job)
    {
        if (job.Invoice is not null) return;

        var invoice = new JobInvoice
        {
            Id = Guid.NewGuid(),
            ServiceJobId = job.Id,
            ServiceJob = job,
            FinalizedAtUtc = job.CreatedAtUtc,
        };
        job.Invoice = invoice;
        db.JobInvoices.Add(invoice);
    }

    private void AppendPurchaseLineIfMissing(JobInvoice invoice, JobPurchaseLine purchase)
    {
        if (invoice.LineItems.Any(l => l.JobPurchaseLineId == purchase.Id))
            return;

        var sortOrder = invoice.LineItems.Count == 0
            ? 0
            : invoice.LineItems.Max(l => l.SortOrder) + 1;

        db.JobInvoiceLineItems.Add(new JobInvoiceLineItem
        {
            Id = Guid.NewGuid(),
            JobInvoice = invoice,
            JobInvoiceId = invoice.Id,
            Kind = InvoiceLineItemKind.Purchase,
            Source = PurchaseSourceMapping.ToLineItemSource(purchase.Source),
            JobPurchaseLineId = purchase.Id,
            JobPurchaseLine = purchase,
            Description = purchase.Description,
            Quantity = purchase.Quantity,
            UnitPrice = purchase.UnitPrice,
            SortOrder = sortOrder,
        });
    }

    private async Task EnsureServiceChargeLineAsync(ServiceJob job, CancellationToken cancellationToken)
    {
        if (job.JobType == JobType.StoreSale) return;
        if (job.Invoice!.LineItems.Any(l => l.Kind == InvoiceLineItemKind.ServiceCharge)) return;

        var configured = await JobTypeServicePriceLookup.GetConfiguredChargeAsync(db, job.JobType, cancellationToken);
        var serviceCharge = JobServiceChargeResolver.EffectiveCharge(job, configured);
        var jobTypeLabel = JobTypeDisplay.Label(job.JobType, job.JobTypeOtherDescription);

        foreach (var line in job.Invoice.LineItems)
            line.SortOrder++;

        db.JobInvoiceLineItems.Add(new JobInvoiceLineItem
        {
            Id = Guid.NewGuid(),
            JobInvoice = job.Invoice,
            JobInvoiceId = job.Invoice.Id,
            Kind = InvoiceLineItemKind.ServiceCharge,
            Source = null,
            Description = $"Service charge — {jobTypeLabel}",
            Quantity = 1,
            UnitPrice = serviceCharge,
            SortOrder = 0,
        });
    }
}