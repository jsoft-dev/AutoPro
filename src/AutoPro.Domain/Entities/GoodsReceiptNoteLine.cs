namespace AutoPro.Domain.Entities;

public class GoodsReceiptNoteLine
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote GoodsReceiptNote { get; set; } = null!;
    public Guid InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}