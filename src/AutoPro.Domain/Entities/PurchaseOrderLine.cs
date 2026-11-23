namespace AutoPro.Domain.Entities;

public class PurchaseOrderLine
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Guid InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}