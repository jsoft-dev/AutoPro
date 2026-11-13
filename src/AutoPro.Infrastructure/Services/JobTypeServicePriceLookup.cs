using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public static class JobTypeServicePriceLookup
{
    public static async Task<decimal> GetConfiguredChargeAsync(
        AutoProDbContext db,
        JobType jobType,
        CancellationToken cancellationToken = default)
    {
        var row = await db.JobTypeServicePrices.AsNoTracking()
            .FirstOrDefaultAsync(p => p.JobType == jobType, cancellationToken);

        return row?.ServiceChargeLkr ?? JobTypeServicePriceDefaults.ServiceChargeLkr(jobType);
    }

    public static async Task<decimal> GetEffectiveChargeAsync(
        AutoProDbContext db,
        ServiceJob job,
        CancellationToken cancellationToken = default)
    {
        var configured = await GetConfiguredChargeAsync(db, job.JobType, cancellationToken);
        return JobServiceChargeResolver.EffectiveCharge(job, configured);
    }
}
