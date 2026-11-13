using AutoPro.Application.Contracts.Jobs;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Services;

namespace AutoPro.Infrastructure.Services;

public static class JobDetailMapper
{
    public static JobDetailResponse ToDetail(
        ServiceJob job,
        decimal jobTypeServiceChargeLkr,
        int? vehicleLastRecordedOdometerKm = null)
    {
        var purchasesTotal = job.PurchaseLines.Sum(p => p.Quantity * p.UnitPrice);

        JobInvoiceResponse? invoice = null;
        if (job.Invoice is not null)
        {
            var lines = job.Invoice.LineItems
                .OrderBy(l => l.SortOrder)
                .Select(l =>
                {
                    JobPurchaseLine? purchase = l.JobPurchaseLine;
                    return new JobInvoiceLineItemResponse(
                        l.Id,
                        l.Kind,
                        l.Source,
                        l.Description,
                        l.Quantity,
                        l.UnitPrice,
                        purchase?.VendorName,
                        purchase?.InventoryItem?.PartNumber);
                })
                .ToList();

            var grandTotal = lines.Sum(li => li.Quantity * li.UnitPrice);
            invoice = new JobInvoiceResponse(
                job.Invoice.Id,
                job.Invoice.FinalizedAtUtc,
                grandTotal,
                lines);
        }

        var customerName = job.Vehicle?.Customer.FullName
            ?? (string.IsNullOrWhiteSpace(job.StoreCustomerName) ? "Walk-in" : job.StoreCustomerName.Trim());

        return new(
            job.Id,
            job.JobNumber,
            job.Status,
            job.JobType,
            job.JobTypeOtherDescription,
            JobTypeDisplay.Label(job.JobType, job.JobTypeOtherDescription),
            job.VehicleId,
            job.Vehicle?.PlateNumber,
            job.Vehicle?.CustomerId,
            customerName,
            job.StoreCustomerName,
            job.OdometerReading,
            job.Notes,
            job.CreatedAtUtc,
            job.CompletedAtUtc,
            job.ClosedAtUtc,
            purchasesTotal,
            job.CustomServiceChargeLkr,
            jobTypeServiceChargeLkr,
            job.PurchaseLines
                .OrderBy(p => p.CreatedAtUtc)
                .Select(p => new JobPurchaseLineResponse(
                    p.Id,
                    p.Source,
                    p.InventoryItemId,
                    p.InventoryItem?.PartNumber,
                    p.Description,
                    p.Quantity,
                    p.UnitPrice,
                    p.VendorName,
                    p.CreatedAtUtc))
                .ToList(),
            invoice,
            job.StatusHistory
                .OrderBy(h => h.ChangedAtUtc)
                .Select(h => new JobStatusHistoryResponse(h.FromStatus, h.ToStatus, h.ChangedAtUtc, h.Note))
                .ToList(),
            vehicleLastRecordedOdometerKm);
    }
}
