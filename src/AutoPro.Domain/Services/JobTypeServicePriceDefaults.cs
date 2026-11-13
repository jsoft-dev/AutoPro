using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Services;

public static class JobTypeServicePriceDefaults
{
    public static decimal ServiceChargeLkr(JobType jobType) =>
        jobType switch
        {
            JobType.Wash => 1500m,
            JobType.FullService => 3500m,
            JobType.Repair => 2000m,
            JobType.Other => 0m,
            _ => 0m,
        };
}
