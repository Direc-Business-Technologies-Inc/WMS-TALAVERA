using Application.DataTransferObjects.Transactions.Commons.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Contract rules 1-2 (resolution plan 2026-10-23, Phase 1.4): a request with
/// nothing to fulfil, or one that identifies more than one order, must fail
/// before the first NetSuite call. The guards are extracted so they are
/// testable without HTTP.
/// </summary>
public class NetSuiteApiClientServiceTOFulfillmentSaveGuardTests
{
    private const int OrderId = 9001;

    private static PostTransferOrderDTO Line(int orderId, decimal scannedQuantity, int lineId = 1) => new()
    {
        NetsuiteOrderInternalId = orderId,
        LineSequenceNumber = lineId,
        ScannedQuantity = scannedQuantity
    };

    [Fact]
    public void An_empty_request_is_rejected()
    {
        var exception = Assert.Throws<Exception>(() =>
            NetSuiteApiClientService.ValidateTOFulfillmentRequest([]));

        Assert.Contains("no positive scanned quantity", exception.Message);
        Assert.Contains("Nothing was saved", exception.Message);
    }

    [Fact]
    public void An_all_zero_request_is_rejected()
    {
        var exception = Assert.Throws<Exception>(() =>
            NetSuiteApiClientService.ValidateTOFulfillmentRequest(
                [Line(OrderId, 0), Line(OrderId, 0, lineId: 2)]));

        Assert.Contains("no positive scanned quantity", exception.Message);
    }

    [Fact]
    public void A_request_mixing_two_orders_is_rejected()
    {
        var exception = Assert.Throws<Exception>(() =>
            NetSuiteApiClientService.ValidateTOFulfillmentRequest(
                [Line(OrderId, 1), Line(OrderId + 1, 2, lineId: 2)]));

        Assert.Contains("exactly one transfer order", exception.Message);
        Assert.Contains("9002", exception.Message);
    }

    [Fact]
    public void A_missing_order_id_is_rejected()
    {
        var exception = Assert.Throws<Exception>(() =>
            NetSuiteApiClientService.ValidateTOFulfillmentRequest([Line(0, 1)]));

        Assert.Contains("exactly one transfer order", exception.Message);
    }

    [Fact]
    public void One_order_with_at_least_one_scan_passes()
    {
        NetSuiteApiClientService.ValidateTOFulfillmentRequest(
            [Line(OrderId, 1), Line(OrderId, 0, lineId: 2)]);
    }
}
