namespace AutoPro.Domain.Entities;

public class JobPurchaseLine
{
    public Guid Id { get; set; }
    public Guid ServiceJobId { get; set; }
    public ServiceJob ServiceJob { get; set; } = null!;

    public Enums.PurchaseSource Source { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? VendorName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid AddedByUserId { get; set; }
}
