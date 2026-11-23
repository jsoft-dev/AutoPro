using AutoPro.Domain.Entities;
using AutoPro.Domain.Services;

namespace AutoPro.Domain.Tests;

public class InventoryQuantityRulesTests
{
  [Fact]
  public void When_receipt_applied_then_quantity_increases()
  {
    var item = new InventoryItem
    {
      PartNumber = "FLT-01",
      Name = "Filter",
      QuantityOnHand = 5,
      IsActive = true
    };

    var error = InventoryQuantityRules.ValidateReceipt(item, 3);
    Assert.Null(error);
    InventoryQuantityRules.ApplyReceipt(item, 3);

    Assert.Equal(8, item.QuantityOnHand);
  }

  [Fact]
  public void When_issue_exceeds_on_hand_then_validation_fails()
  {
    var item = new InventoryItem
    {
      PartNumber = "FLT-01",
      Name = "Filter",
      QuantityOnHand = 2,
      IsActive = true
    };

    var error = InventoryQuantityRules.ValidateIssue(item, 3);
    Assert.Contains("Insufficient stock", error);
  }
}