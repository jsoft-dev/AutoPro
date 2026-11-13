namespace AutoPro.Domain.Entities;

public class JobInvoice
{
    public Guid Id { get; set; }
    public Guid ServiceJobId { get; set; }
    public ServiceJob ServiceJob { get; set; } = null!;

    public DateTime FinalizedAtUtc { get; set; }

    public ICollection<JobInvoiceLineItem> LineItems { get; set; } = new List<JobInvoiceLineItem>();
}
