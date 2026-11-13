namespace AutoPro.Domain.Enums;

/// <summary>
/// How a billable line is fulfilled: in-house stock/labor vs outsourced vendor.
/// </summary>
public enum LineItemSource
{
    InHouse = 0,
    OutSource = 1,
}
