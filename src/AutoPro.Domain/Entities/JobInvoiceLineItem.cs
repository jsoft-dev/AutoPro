using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Entities;

public class JobInvoiceLineItem
{
    public Guid Id { get; set; }
    public Guid JobInvoiceId { get; set; }
    public JobInvoice JobInvoice { get; set; } = null!;

    public InvoiceLineItemKind Kind { get; set; }
    public LineItemSource? Source { get; set; }

    public Guid? JobPurchaseLineId { get; set; }
    public JobPurchaseLine? JobPurchaseLine { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int SortOrder { get; set; }
}
