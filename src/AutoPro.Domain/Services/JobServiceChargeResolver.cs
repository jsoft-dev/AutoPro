using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Services;

public static class JobServiceChargeResolver
{
    public static decimal EffectiveCharge(ServiceJob job, decimal configuredChargeForJobType)
    {
        if (job.JobType == JobType.StoreSale)
            return 0m;

        return job.JobType == JobType.Other && job.CustomServiceChargeLkr is decimal custom
            ? custom
            : configuredChargeForJobType;
    }
}
