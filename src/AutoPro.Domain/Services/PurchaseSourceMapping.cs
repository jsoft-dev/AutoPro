using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Services;

public static class PurchaseSourceMapping
{
    public static LineItemSource ToLineItemSource(PurchaseSource source) =>
        source switch
        {
            PurchaseSource.Inventory => LineItemSource.InHouse,
            PurchaseSource.ExternalVendor => LineItemSource.OutSource,
            _ => LineItemSource.InHouse,
        };
}
