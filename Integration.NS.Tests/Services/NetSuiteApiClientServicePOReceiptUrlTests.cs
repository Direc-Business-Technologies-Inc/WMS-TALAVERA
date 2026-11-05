using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// The PO item receipt transform must declare
/// <c>?replace=item.inventoryDetail.inventoryAssignment</c> on bin-tracked locations so
/// NetSuite replaces its pre-generated (empty) inventory assignment with the bin the
/// payload carries. Without it NetSuite ignores the body's <c>inventoryDetail</c> and
/// rejects the save with "Please configure the inventory detail in line N". Non-bin
/// locations need no detail and keep the plain transform - matching the Returns receipt,
/// which posts the same <c>itemReceipt</c> transform with the replace param.
/// </summary>
public class NetSuiteApiClientServicePOReceiptUrlTests
{
    [Fact]
    public void Bin_tracked_location_declares_inventory_assignment_replace()
    {
        var url = NetSuiteApiClientService.BuildPOItemReceiptUrl(isUsedBin: true, orderId: 42);

        Assert.Contains("/purchaseOrder/42/!transform/itemReceipt", url);
        Assert.Contains("?replace=item.inventoryDetail.inventoryAssignment", url);
    }

    [Fact]
    public void Non_bin_location_keeps_the_plain_transform()
    {
        var url = NetSuiteApiClientService.BuildPOItemReceiptUrl(isUsedBin: false, orderId: 42);

        Assert.Contains("/purchaseOrder/42/!transform/itemReceipt", url);
        Assert.DoesNotContain("replace=", url);
    }
}
