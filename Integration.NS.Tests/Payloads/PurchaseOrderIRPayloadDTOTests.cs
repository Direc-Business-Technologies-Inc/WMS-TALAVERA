using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Xunit;

namespace Integration.NS.Tests.Payloads;

public class PurchaseOrderIRPayloadDTOTests
{
    private const int UserId = 42;
    private const string Remarks = "Created via WMS";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private static PostPurchaseOrderDTO Line(
        int orderLine,
        bool isBad,
        decimal scannedQuantity,
        bool isLocationUsedBin = false,
        int vendorBinAssignmentId = 0,
        int preferredBinId = 0)
    {
        return new PostPurchaseOrderDTO
        {
            LineSequenceNumber = orderLine,
            IsBad = isBad,
            ScannedQuantity = scannedQuantity,
            PhysicalQuantity = scannedQuantity,
            TotalWeight = scannedQuantity * 2,
            ScannedWeight = scannedQuantity * 3,
            IsLocationUsedBin = isLocationUsedBin,
            VendorBinAssignmentId = vendorBinAssignmentId,
            NetsuiteMaterialPrefferedBinId = preferredBinId
        };
    }

    private static JsonDocument Serialize(List<PostPurchaseOrderDTO> lines, int receivingCategory)
    {
        var payload = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(lines, UserId, receivingCategory, Remarks);
        var json = JsonSerializer.Serialize(payload, Options);
        return JsonDocument.Parse(json);
    }

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

    private static IReadOnlyList<JsonElement> Items(JsonDocument document)
        => document.RootElement.GetProperty("item").GetProperty("items").EnumerateArray().ToList();

    private static void AssertNotReceived(JsonElement line, int orderLine)
    {
        Assert.Equal(orderLine, line.GetProperty("orderLine").GetInt32());
        Assert.False(line.GetProperty("itemreceive").GetBoolean());
        Assert.False(line.TryGetProperty("quantity", out _));
        Assert.False(line.TryGetProperty("inventoryDetail", out _));
        Assert.False(line.TryGetProperty("rate", out _));
        Assert.False(line.TryGetProperty("custcol_dbti_record_weight", out _));
        Assert.False(line.TryGetProperty("custcol_dbti_actual_weight", out _));
        Assert.False(line.TryGetProperty("custcol_dbti_actual_qty", out _));
        Assert.False(line.TryGetProperty("location", out _));
    }

    private static List<PostPurchaseOrderDTO> MixedPo() => new()
    {
        Line(1, isBad: false, scannedQuantity: 10),
        Line(2, isBad: false, scannedQuantity: 0),
        Line(3, isBad: true, scannedQuantity: 2)
    };

    [Fact]
    public void Good_payload_emits_entry_for_every_open_line()
    {
        using var document = Serialize(MixedPo(), 1);

        Assert.Equal(3, Items(document).Count);
        Assert.Equal(3, document.RootElement.GetProperty("item").GetProperty("items").GetArrayLength());
    }

    [Fact]
    public void Good_payload_receives_only_selected_good_lines()
    {
        using var document = Serialize(MixedPo(), 1);

        var received = LineEntry(document, 1);
        Assert.True(received.GetProperty("itemreceive").GetBoolean());
        Assert.Equal(10m, received.GetProperty("quantity").GetDecimal());
        Assert.True(received.TryGetProperty("inventoryDetail", out _));

        AssertNotReceived(LineEntry(document, 2), 2);
        AssertNotReceived(LineEntry(document, 3), 3);
    }

    [Fact]
    public void Bad_payload_receives_only_selected_bad_lines()
    {
        using var document = Serialize(MixedPo(), 2);

        var received = LineEntry(document, 3);
        Assert.True(received.GetProperty("itemreceive").GetBoolean());
        Assert.Equal(2m, received.GetProperty("quantity").GetDecimal());
        Assert.True(received.TryGetProperty("inventoryDetail", out _));

        AssertNotReceived(LineEntry(document, 1), 1);
        AssertNotReceived(LineEntry(document, 2), 2);
    }

    [Fact]
    public void Good_and_bad_payloads_receive_disjoint_lines()
    {
        using var good = Serialize(MixedPo(), 1);
        using var bad = Serialize(MixedPo(), 2);

        var goodReceived = Items(good)
            .Where(x => x.GetProperty("itemreceive").GetBoolean())
            .Select(x => x.GetProperty("orderLine").GetInt32())
            .ToHashSet();
        var badReceived = Items(bad)
            .Where(x => x.GetProperty("itemreceive").GetBoolean())
            .Select(x => x.GetProperty("orderLine").GetInt32())
            .ToHashSet();

        Assert.Empty(goodReceived.Intersect(badReceived));
        Assert.Equal(new[] { 1, 3 }, goodReceived.Union(badReceived).OrderBy(x => x).ToArray());
    }

    // Regression — the mobile client sends a split line twice (good row + bad row,
    // same LineSequenceNumber). NetSuite rejects the resulting duplicate orderLine
    // entries with [DUPLICATE_KEYS].
    [Fact]
    public void Line_split_between_good_and_bad_yields_a_single_entry_per_payload()
    {
        var split = new List<PostPurchaseOrderDTO>
        {
            Line(1, isBad: false, scannedQuantity: 8),
            Line(1, isBad: true, scannedQuantity: 2)
        };

        using var good = Serialize(split, 1);
        using var bad = Serialize(split, 2);

        Assert.Equal(1, Items(good).Count);
        Assert.Equal(1, Items(bad).Count);

        var goodLine = LineEntry(good, 1);
        Assert.True(goodLine.GetProperty("itemreceive").GetBoolean());
        Assert.Equal(8m, goodLine.GetProperty("quantity").GetDecimal());

        var badLine = LineEntry(bad, 1);
        Assert.True(badLine.GetProperty("itemreceive").GetBoolean());
        Assert.Equal(2m, badLine.GetProperty("quantity").GetDecimal());
    }

    [Fact]
    public void Line_scanned_only_as_good_is_not_received_by_the_bad_post()
    {
        var lines = new List<PostPurchaseOrderDTO> { Line(1, isBad: false, scannedQuantity: 8) };

        using var bad = Serialize(lines, 2);

        Assert.Equal(1, Items(bad).Count);
        AssertNotReceived(LineEntry(bad, 1), 1);
    }

    [Fact]
    public void Line_scanned_only_as_bad_is_not_received_by_the_good_post()
    {
        var lines = new List<PostPurchaseOrderDTO> { Line(1, isBad: true, scannedQuantity: 2) };

        using var good = Serialize(lines, 1);

        Assert.Equal(1, Items(good).Count);
        AssertNotReceived(LineEntry(good, 1), 1);
    }

    [Fact]
    public void Single_line_fully_received_payload_matches_current_shape()
    {
        using var document = Serialize(new List<PostPurchaseOrderDTO> { Line(1, isBad: false, scannedQuantity: 10) }, 1);

        var actual = JsonNode.Parse(document.RootElement.GetRawText());
        var expected = JsonNode.Parse(
            """
            {
              "custbody_dbti_created_in_wms": true,
              "custbody_dbti_receiving_category": 1,
              "custbody_dbti_received_by": 42,
              "memo": "Created via WMS",
              "item": {
                "items": [
                  {
                    "orderLine": 1,
                    "itemreceive": true,
                    "quantity": 10,
                    "custcol_dbti_actual_qty": 10,
                    "custcol_dbti_record_weight": 20,
                    "custcol_dbti_actual_weight": 30,
                    "inventoryDetail": {
                      "inventoryAssignment": {
                        "items": [
                          {
                            "inventoryStatus": { "id": "1" },
                            "quantity": 10
                          }
                        ]
                      }
                    }
                  }
                ]
              }
            }
            """);

        Assert.True(JsonNode.DeepEquals(expected, actual));
    }

    [Fact]
    public void Received_line_resolves_bin_number_from_vendor_assignment()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(1, isBad: false, scannedQuantity: 5, isLocationUsedBin: true, vendorBinAssignmentId: 555)
        };

        using var document = Serialize(lines, 1);

        var binNumber = LineEntry(document, 1)
            .GetProperty("inventoryDetail")
            .GetProperty("inventoryAssignment")
            .GetProperty("items")[0]
            .GetProperty("binNumber");

        Assert.Equal("555", binNumber.GetProperty("id").GetString());
    }

    [Fact]
    public void Received_line_resolves_bin_number_from_preferred_bin_when_no_vendor_assignment()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(1, isBad: false, scannedQuantity: 5, isLocationUsedBin: true, preferredBinId: 777)
        };

        using var document = Serialize(lines, 1);

        var binNumber = LineEntry(document, 1)
            .GetProperty("inventoryDetail")
            .GetProperty("inventoryAssignment")
            .GetProperty("items")[0]
            .GetProperty("binNumber");

        Assert.Equal("777", binNumber.GetProperty("id").GetString());
    }

    [Fact]
    public void Received_bad_line_uses_zero_rate()
    {
        var lines = new List<PostPurchaseOrderDTO> { Line(1, isBad: true, scannedQuantity: 2) };

        using var document = Serialize(lines, 2);

        Assert.Equal(0m, LineEntry(document, 1).GetProperty("rate").GetDecimal());
    }

    [Fact]
    public void Received_good_line_omits_rate()
    {
        var lines = new List<PostPurchaseOrderDTO> { Line(1, isBad: false, scannedQuantity: 2) };

        using var document = Serialize(lines, 1);

        Assert.False(LineEntry(document, 1).TryGetProperty("rate", out _));
    }

    [Fact]
    public void Zero_quantity_only_list_produces_all_false_entries()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(1, isBad: false, scannedQuantity: 0),
            Line(2, isBad: true, scannedQuantity: 0)
        };

        using var document = Serialize(lines, 1);

        Assert.Equal(2, Items(document).Count);
        AssertNotReceived(LineEntry(document, 1), 1);
        AssertNotReceived(LineEntry(document, 2), 2);
    }

    // Regression - the receiving query fans one PO line into several rows when the item
    // has more than one preferred bin at the location or the vendor has several bin
    // assignments. Only one of those rows carries the operator's scan; the payload must
    // receive THAT row, not an unscanned duplicate that would silently drop the receipt
    // (or emit a null bin and trip NetSuite's "configure the inventory detail").
    [Fact]
    public void Fanned_out_line_receives_the_scanned_duplicate_and_its_bin()
    {
        var lines = new List<PostPurchaseOrderDTO>
        {
            Line(1, isBad: false, scannedQuantity: 0, isLocationUsedBin: true, vendorBinAssignmentId: 0, preferredBinId: 0),
            Line(1, isBad: false, scannedQuantity: 4, isLocationUsedBin: true, vendorBinAssignmentId: 55, preferredBinId: 0)
        };

        using var document = Serialize(lines, 1);

        Assert.Equal(1, Items(document).Count);

        var received = LineEntry(document, 1);
        Assert.True(received.GetProperty("itemreceive").GetBoolean());
        Assert.Equal(4m, received.GetProperty("quantity").GetDecimal());

        var binNumber = received
            .GetProperty("inventoryDetail")
            .GetProperty("inventoryAssignment")
            .GetProperty("items")[0]
            .GetProperty("binNumber");

        Assert.Equal("55", binNumber.GetProperty("id").GetString());
    }
}
