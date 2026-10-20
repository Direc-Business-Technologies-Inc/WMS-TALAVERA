using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS.Payload;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions.InventoryCounting;

/// <summary>
/// Guards the inventory-count PATCH payload against the two field defects reported in 2026-10:
/// untouched count lines were sent as quantity 0, and Bad quantities were dropped entirely.
/// A line is "touched" only when the operator acted on it (<see cref="PatchInventoryCountingDTO.IsCounted"/>)
/// or when it carries a quantity (the web page never sets IsCounted).
/// </summary>
public class PatchInventoryCountingPayloadTests
{
    private static PatchInventoryCountingDTO Line(
        int lineId,
        decimal quantity,
        bool isBad = false,
        bool isCounted = true,
        int? detailId = null) => new()
        {
            NetsuiteOrderInternalId = 100,
            LineSequenceNumber = lineId,
            ScannedQuantity = quantity,
            IsBad = isBad,
            IsCounted = isCounted,
            NetsuiteInventoryDetailInternalId = detailId
        };

    [Fact]
    public void Untouched_lines_are_not_sent()
    {
        var payload = PatchInventoryCountingPayloadDTO.PatchInventoryCounting(
        [
            Line(1, 0, isCounted: false),
            Line(2, 5, detailId: 55)
        ]);

        var item = Assert.Single(payload.Item.Items);
        Assert.Equal(2, item.CountLine);
        Assert.Equal(5, item.CountQuantity);
    }

    [Fact]
    public void Line_counted_as_zero_is_sent_with_one_zero_detail_row()
    {
        var payload = PatchInventoryCountingPayloadDTO.PatchInventoryCounting(
        [
            Line(1, 0),
            Line(1, 0, isBad: true)
        ]);

        var item = Assert.Single(payload.Item.Items);
        var detail = Assert.Single(item.CountDetail!.CountInventoryDetail.Items);

        Assert.Equal(0, item.CountQuantity);
        Assert.Equal(0, detail.Quantity);
        Assert.Equal(InventoryStatusMap.Good, detail.InventoryStatus.Id);
        Assert.Null(detail.Id);
    }

    [Fact]
    public void Good_and_bad_are_sent_as_separate_detail_rows()
    {
        var payload = PatchInventoryCountingPayloadDTO.PatchInventoryCounting(
        [
            Line(7, 3, detailId: 70),
            Line(7, 2, isBad: true, detailId: 71)
        ]);

        var item = Assert.Single(payload.Item.Items);
        var details = item.CountDetail!.CountInventoryDetail.Items;

        Assert.Equal(5, item.CountQuantity);
        Assert.Equal(2, details.Count);

        Assert.Equal(InventoryStatusMap.Good, details[0].InventoryStatus.Id);
        Assert.Equal(3, details[0].Quantity);
        Assert.Equal(70, details[0].Id);

        Assert.Equal(InventoryStatusMap.Bad, details[1].InventoryStatus.Id);
        Assert.Equal(2, details[1].Quantity);
        Assert.Equal(71, details[1].Id);
    }

    [Fact]
    public void Bad_only_line_is_sent_as_status_bad()
    {
        var payload = PatchInventoryCountingPayloadDTO.PatchInventoryCounting(
        [
            Line(9, 0),
            Line(9, 4, isBad: true)
        ]);

        var item = Assert.Single(payload.Item.Items);
        var detail = Assert.Single(item.CountDetail!.CountInventoryDetail.Items);

        Assert.Equal(4, item.CountQuantity);
        Assert.Equal(InventoryStatusMap.Bad, detail.InventoryStatus.Id);
        Assert.Equal(4, detail.Quantity);
    }

    [Fact]
    public void Web_path_line_with_quantity_and_no_is_counted_flag_is_still_sent()
    {
        var payload = PatchInventoryCountingPayloadDTO.PatchInventoryCounting(
        [
            Line(3, 6, isCounted: false, detailId: 33)
        ]);

        var item = Assert.Single(payload.Item.Items);

        Assert.Equal(6, item.CountQuantity);
        Assert.Equal(33, item.CountDetail!.CountInventoryDetail.Items[0].Id);
    }
}
