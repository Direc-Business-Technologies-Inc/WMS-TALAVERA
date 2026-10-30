using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Commands.Transaction.StockTransferRequest;
using Xunit;

namespace Integration.NS.Tests.Helpers;

public class AutoFulfillmentPreflightTests
{
    private static AutoFulfillmentStockLineDTO Line(int lineId, int itemId, decimal available, decimal ordered = 4,
        decimal shipped = 0, int? status = 1) =>
        new(lineId, itemId, 10, status, 20, true, ordered, shipped, available, 1, false);

    [Fact]
    public void One_invalid_line_blocks_the_entire_document_before_posting()
    {
        var result = AutoFulfillmentPreflight.Check([Line(1, 11, 10), Line(2, 12, 0)], 2);

        Assert.Equal(AutoFulfillmentPreflightDecision.Blocked, result.Decision);
        Assert.Empty(result.RemainingBaseQuantityByLine);
    }

    [Fact]
    public void Two_order_lines_cannot_spend_the_same_stock_pool_twice()
    {
        var result = AutoFulfillmentPreflight.Check([Line(1, 11, 5), Line(2, 11, 5)], 2);

        Assert.Equal(AutoFulfillmentPreflightDecision.Blocked, result.Decision);
        Assert.Empty(result.RemainingBaseQuantityByLine);
    }

    [Fact]
    public void Already_shipped_quantity_is_excluded_from_a_valid_partial_order()
    {
        var result = AutoFulfillmentPreflight.Check([Line(1, 11, 3, ordered: 8, shipped: 5)], 1);

        Assert.Equal(AutoFulfillmentPreflightDecision.Ready, result.Decision);
        Assert.Equal(3, result.RemainingBaseQuantityByLine[1]);
    }

    [Fact]
    public void Unknown_or_incomplete_line_set_and_unresolved_quality_fail_closed()
    {
        Assert.Equal(AutoFulfillmentPreflightDecision.Blocked,
            AutoFulfillmentPreflight.Check([Line(1, 11, 10)], null).Decision);
        Assert.Equal(AutoFulfillmentPreflightDecision.Blocked,
            AutoFulfillmentPreflight.Check([Line(1, 11, 10)], 2).Decision);
        Assert.Equal(AutoFulfillmentPreflightDecision.Blocked,
            AutoFulfillmentPreflight.Check([Line(1, 11, 10, status: null)], 1).Decision);
    }

    [Fact]
    public void Closed_order_has_no_quantities_left_to_auto_ship()
    {
        var result = AutoFulfillmentPreflight.Check([Line(1, 11, 0, ordered: 4, shipped: 4)], 0);

        Assert.Equal(AutoFulfillmentPreflightDecision.NoOpenLines, result.Decision);
        Assert.Empty(result.RemainingBaseQuantityByLine);
    }
}
