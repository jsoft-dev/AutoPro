using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Entities;

public class JobTypeServicePrice
{
    public Guid Id { get; set; }
    public JobType JobType { get; set; }
    public decimal ServiceChargeLkr { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
