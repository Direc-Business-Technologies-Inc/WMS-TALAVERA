using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Packing.NS.Payload;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Xunit;

namespace Integration.NS.Tests.Payloads;

/// <summary>
/// Pins the inventory status written by every packing payload builder.
///
/// Background: the packing builders resolved status from the order-level transfer category
/// (<c>TransferCategory == 3 ? "1" : "3"</c>), which silently booked operator-marked bad goods
/// as Good stock on a Returns-Good order. All builders now resolve through
/// <see cref="InventoryStatusMap"/>.
/// </summary>
public class PackingIFPayloadDTOTests
{
    private const string ShipStatus = "B";
    private const bool UseBin = true;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private static PostReturnsDTO ReturnsLine(
        int orderLine,
        int transferCategory = InventoryStatusMap.ReturnCategoryGood,
        bool isBad = false,
        bool isMissing = false,
        decimal scannedQuantity = 1)
        => new()
        {
            LineSequenceNumber = orderLine,
            TransferCategory = transferCategory,
            IsBad = isBad,
            IsMissing = isMissing,
            IsLocationUsedBin = true,
            NetsuiteMaterialPrefferedBinId = 777,
            ScannedQuantity = scannedQuantity
        };

    private static PostTransferOrderDTO TransferOrderLine(
        int orderLine,
        bool isBad = false,
        bool isMissing = false,
        decimal scannedQuantity = 1)
        => new()
        {
            LineSequenceNumber = orderLine,
            IsBad = isBad,
            IsMissing = isMissing,
            IsLocationUsedBin = true,
            NetsuiteMaterialPrefferedBinId = 777,
            ScannedQuantity = scannedQuantity
        };

    private static PostVendorReturnAuthorizationDTO SraLine(
        int orderLine,
        bool isBad = false,
        decimal scannedQuantity = 1)
        => new()
        {
            LineSequenceNumber = orderLine,
            IsBad = isBad,
            IsLocationUsedBin = true,
            NetsuiteMaterialPrefferedBinId = 777,
            ScannedQuantity = scannedQuantity
        };

    private static string StatusId(JsonElement line) =>
        line.GetProperty("inventoryDetail")
            .GetProperty("inventoryAssignment")
            .GetProperty("items")[0]
            .GetProperty("inventoryStatus")
            .GetProperty("id")
            .GetString()!;

    private static JsonElement LineEntry(JsonDocument document, int orderLine)
    {
        foreach (var item in document.RootElement.GetProperty("item").GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("orderLine").GetInt32() == orderLine)
            {
                return item;
            }
        }

        throw new InvalidOperationException($"No item entry found for orderLine {orderLine}.");
    }

    private static JsonDocument SerializeReturns(PostReturnsDTO line)
    {
        var payload = ReturnsIFPayloadDTO.CreateForItemFulfillment(
            [line], ShipStatus, printLabel: true, UseBin);

        return JsonDocument.Parse(JsonSerializer.Serialize(payload, Options));
    }

    private static JsonDocument SerializeTransferOrder(PostTransferOrderDTO line)
    {
        var payload = TransferOrderIFPayloadDTO.CreateForItemFulfillment(
            [line], ShipStatus, printLabel: true, UseBin);

        return JsonDocument.Parse(JsonSerializer.Serialize(payload, Options));
    }

    private static JsonDocument SerializeVra(PostVendorReturnAuthorizationDTO line)
    {
        var payload = VendorReturnAuthorizationIFPayloadDTO.CreateForItemFulfillment(
            [line], ShipStatus, printLabel: true, UseBin);

        return JsonDocument.Parse(JsonSerializer.Serialize(payload, Options));
    }

    [Theory]
    [InlineData(InventoryStatusMap.ReturnCategoryGood, false, false, InventoryStatusMap.Good)]
    [InlineData(InventoryStatusMap.ReturnCategoryGood, true, false, InventoryStatusMap.Bad)]
    [InlineData(InventoryStatusMap.ReturnCategoryBad, false, false, InventoryStatusMap.Bad)]
    [InlineData(InventoryStatusMap.ReturnCategoryBad, true, false, InventoryStatusMap.Bad)]
    [InlineData(InventoryStatusMap.ReturnCategoryGood, false, true, InventoryStatusMap.Missing)]
    [InlineData(InventoryStatusMap.ReturnCategoryBad, true, true, InventoryStatusMap.Missing)]
    public void Returns_resolves_status_from_line_condition_first(
        int transferCategory, bool isBad, bool isMissing, string expected)
    {
        var line = ReturnsLine(1, transferCategory, isBad, isMissing);

        using var document = SerializeReturns(line);

        Assert.Equal(expected, StatusId(LineEntry(document, 1)));
    }

    [Fact]
    public void Returns_books_operator_marked_bad_line_on_a_good_order_as_bad()
    {
        // Regression guard. The old category-first ternary sent "1" (Good) here, so damage on a
        // Returns-Good order was accepted by NetSuite with no error and silently overstated stock.
        var line = ReturnsLine(1, transferCategory: InventoryStatusMap.ReturnCategoryGood, isBad: true);

        using var document = SerializeReturns(line);

        Assert.Equal(InventoryStatusMap.Bad, StatusId(LineEntry(document, 1)));
    }

    [Theory]
    [InlineData(false, false, InventoryStatusMap.Good)]
    [InlineData(true, false, InventoryStatusMap.Bad)]
    [InlineData(false, true, InventoryStatusMap.Missing)]
    [InlineData(true, true, InventoryStatusMap.Missing)]
    public void Transfer_order_resolves_status_from_line_condition(bool isBad, bool isMissing, string expected)
    {
        var line = TransferOrderLine(1, isBad, isMissing);

        using var document = SerializeTransferOrder(line);

        Assert.Equal(expected, StatusId(LineEntry(document, 1)));
    }

    [Theory]
    [InlineData(false, InventoryStatusMap.Good)]
    [InlineData(true, InventoryStatusMap.Bad)]
    public void Vendor_return_authorization_resolves_status_from_condition(bool isBad, string expected)
    {
        var line = SraLine(1, isBad);

        using var document = SerializeVra(line);

        Assert.Equal(expected, StatusId(LineEntry(document, 1)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Packing_and_receiving_agree_for_a_returns_good_order(bool isBad, bool isMissing)
    {
        // Guards against the two paths drifting apart again: receiving already had the correct
        // per-line mapping, packing did not. For a Returns-Good order they must now be identical.
        var packingLine = ReturnsLine(
            1, InventoryStatusMap.ReturnCategoryGood, isBad, isMissing);

        var receivingPayload = ReturnsIRPayloadDTO.CreateForItemReceipt(
            [packingLine], receivingCategory: 1, ifOrderId: 99, userId: 42, remarks: "test");

        using var packing = SerializeReturns(packingLine);
        using var receiving = JsonDocument.Parse(JsonSerializer.Serialize(receivingPayload, Options));

        Assert.Equal(
            StatusId(LineEntry(receiving, 1)),
            StatusId(LineEntry(packing, 1)));
    }

    [Fact]
    public void Returns_zero_quantity_line_emits_no_inventory_status()
    {
        var line = ReturnsLine(1, isBad: true, scannedQuantity: 0);

        using var document = SerializeReturns(line);

        var entry = LineEntry(document, 1);
        Assert.False(entry.GetProperty("itemreceive").GetBoolean());
        Assert.False(entry.TryGetProperty("inventoryStatus", out _));
    }

    [Fact]
    public void Returns_non_bin_location_sends_no_inventory_detail()
    {
        var line = ReturnsLine(1, isBad: true);
        line.IsLocationUsedBin = false;

        var payload = ReturnsIFPayloadDTO.CreateForItemFulfillment(
            [line], ShipStatus, printLabel: true, isUsedBin: false);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(payload, Options));

        Assert.False(LineEntry(document, 1).TryGetProperty("inventoryDetail", out _));
    }
}