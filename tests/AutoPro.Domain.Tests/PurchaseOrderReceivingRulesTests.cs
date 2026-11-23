using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;

namespace AutoPro.Domain.Tests;

public class PurchaseOrderReceivingRulesTests
{
  [Fact]
  public void When_receive_exceeds_remaining_then_validation_fails()
  {
    var line = new PurchaseOrderLine { QuantityOrdered = 10, QuantityReceived = 4 };
    var error = PurchaseOrderReceivingRules.ValidateReceiveQuantity(line, 7);
    Assert.NotNull(error);
  }

  [Fact]
  public void When_all_lines_received_then_status_is_closed()
  {
    var lines = new List<PurchaseOrderLine>
    {
      new() { QuantityOrdered = 5, QuantityReceived = 5 },
      new() { QuantityOrdered = 2, QuantityReceived = 2 }
    };
    Assert.Equal(PurchaseOrderStatus.Closed, PurchaseOrderReceivingRules.RollUpStatus(lines));
  }

  [Fact]
  public void When_partial_receive_then_status_is_partially_received()
  {
    var lines = new List<PurchaseOrderLine>
    {
      new() { QuantityOrdered = 5, QuantityReceived = 2 }
    };
    Assert.Equal(PurchaseOrderStatus.PartiallyReceived, PurchaseOrderReceivingRules.RollUpStatus(lines));
  }
}