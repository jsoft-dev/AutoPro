namespace AutoPro.Domain.Entities;

public class ServiceJob
{
    public Guid Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;

    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    /// <summary>Walk-in or counter-sale customer when there is no linked vehicle.</summary>
    public string? StoreCustomerName { get; set; }

    public Guid CreatedByUserId { get; set; }
    public StaffUser CreatedByUser { get; set; } = null!;

    public int? OdometerReading { get; set; }
    public Enums.JobType JobType { get; set; }
    public string? JobTypeOtherDescription { get; set; }
    /// <summary>Service charge when <see cref="JobType"/> is Other; ignored for standard types.</summary>
    public decimal? CustomServiceChargeLkr { get; set; }
    public Enums.JobStatus Status { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public JobInvoice? Invoice { get; set; }

    public ICollection<JobPurchaseLine> PurchaseLines { get; set; } = new List<JobPurchaseLine>();
    public ICollection<JobStatusHistory> StatusHistory { get; set; } = new List<JobStatusHistory>();
}
