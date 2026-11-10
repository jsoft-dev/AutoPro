namespace AutoPro.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Unique token encoded in the per-vehicle QR code.</summary>
    public string QrToken { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;
    public string? Vin { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<ServiceJob> Jobs { get; set; } = new List<ServiceJob>();
}
