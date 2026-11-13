using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Services;

public static class JobTypeDisplay
{
    public static string Label(JobType type, string? otherDescription) =>
        type == JobType.Other && !string.IsNullOrWhiteSpace(otherDescription)
            ? otherDescription.Trim()
            : type switch
            {
                JobType.Wash => "Wash",
                JobType.FullService => "Full Service",
                JobType.Repair => "Repair",
                JobType.Other => "Other",
                JobType.StoreSale => "Store sale",
                _ => type.ToString(),
            };
}
