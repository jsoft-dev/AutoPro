namespace AutoPro.Domain.Entities;

public class JobStatusHistory
{
    public Guid Id { get; set; }
    public Guid ServiceJobId { get; set; }
    public ServiceJob ServiceJob { get; set; } = null!;

    public Enums.JobStatus FromStatus { get; set; }
    public Enums.JobStatus ToStatus { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string? Note { get; set; }
}
