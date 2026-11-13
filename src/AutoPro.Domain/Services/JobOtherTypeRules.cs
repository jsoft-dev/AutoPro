using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Services;

public static class JobOtherTypeRules
{
    public static string? Validate(JobType jobType, string? otherDescription, decimal? customServiceChargeLkr)
    {
        if (jobType != JobType.Other)
            return null;

        if (string.IsNullOrWhiteSpace(otherDescription))
            return "Describe the job type when Other is selected.";

        if (customServiceChargeLkr is null)
            return "Enter a service charge for this custom job type.";

        if (customServiceChargeLkr < 0)
            return "Service charge cannot be negative.";

        return null;
    }
}
