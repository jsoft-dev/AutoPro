using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = new List<GoodsReceiptNote>();
}