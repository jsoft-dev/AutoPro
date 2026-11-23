using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Entities;

public class GoodsReceiptNote
{
    public Guid Id { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public GoodsReceiptNoteStatus Status { get; set; } = GoodsReceiptNoteStatus.Draft;
    public DateTime ReceivedAtUtc { get; set; }
    public string? SupplierReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? PostedByUserId { get; set; }

    public ICollection<GoodsReceiptNoteLine> Lines { get; set; } = new List<GoodsReceiptNoteLine>();
}