using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Packing.NS.Payload;
using Xunit;

namespace Integration.NS.Tests.Payloads;

/// <summary>
/// Pins the transfer-order item fulfillment RESTlet payload (NetSuite script 2522).
///
/// Unlike the REST <c>!transform/itemFulfillment</c> contract this payload carries only
/// the scanned lines: no zero-quantity fillers and no <c>?replace=</c> parameter, which
/// is what removes the static-sublist errors that blocked orders 226006 / 000881.
/// </summary>
public class TransferOrderIFRestletPayloadDTOTests
{
    private const int TransferOrderId = 12345;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static PostTransferOrderDTO Line(
        int orderLine,
        decimal scannedQuantity,
        bool isBad = false,
        int vendorBin = 0,
        int preferredBin = 0,
        int transferCategory = 1)
        => new()
        {
            NetsuiteOrderInternalId = TransferOrderId,
            OrderNumber = "STR-TEST",
            LineSequenceNumber = orderLine,
            IsBad = isBad,
            IsMissing = false,
            TransferCategory = transferCategory,
            ScannedQuantity = scannedQuantity,
            IsLocationUsedBin = true,
            NetsuiteMaterialVendorAssignedBin = vendorBin,
            NetsuiteMaterialPrefferedBinId = preferredBin
        };

    private static JsonDocument Serialize(List<PostTransferOrderDTO> lines, bool isUsedBin = true)
    {
        var payload = TransferOrderIFRestletPayloadDTO.CreateForItemFulfillmentRestlet(
            lines, TransferOrderId, "B", isUsedBin);

        return JsonDocument.Parse(JsonSerializer.Serialize(payload, Options));
    }

    [Fact]
    public void Fillers_are_never_sent()
    {
        using var document = Serialize([Line(1, 2), Line(2, 0)]);

        var lines = document.RootElement.GetProperty("lines").EnumerateArray().ToList();

        Assert.Single(lines);
        Assert.Equal(1, lines[0].GetProperty("orderLine").GetInt32());
        Assert.Equal(2m, lines[0].GetProperty("quantity").GetDecimal());
    }

    [Fact]
    public void Split_quality_becomes_two_assignments_on_one_line()
    {
        using var document = Serialize([
            Line(7, 3, isBad: true, preferredBin: 202),
            Line(7, 7, isBad: false, preferredBin: 202)
        ]);

        var line = Assert.Single(document.RootElement.GetProperty("lines").EnumerateArray());

        Assert.Equal(7, line.GetProperty("orderLine").GetInt32());
        Assert.Equal(10m, line.GetProperty("quantity").GetDecimal());

        var assignments = line.GetProperty("inventoryAssignments").EnumerateArray().ToList();
        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, x => x.GetProperty("inventoryStatus").GetInt32() == 3 && x.GetProperty("quantity").GetDecimal() == 3m);
        Assert.Contains(assignments, x => x.GetProperty("inventoryStatus").GetInt32() == 1 && x.GetProperty("quantity").GetDecimal() == 7m);
    }

    [Fact]
    public void Vendor_bin_wins_over_preferred_bin_and_falls_back_when_absent()
    {
        using var document = Serialize([
            Line(1, 1, vendorBin: 201, preferredBin: 999),
            Line(2, 1, vendorBin: 0, preferredBin: 202)
        ]);

        var lines = document.RootElement.GetProperty("lines").EnumerateArray().ToList();

        Assert.Equal(201, lines[0].GetProperty("inventoryAssignments")[0].GetProperty("binNumber").GetInt32());
        Assert.Equal(202, lines[1].GetProperty("inventoryAssignments")[0].GetProperty("binNumber").GetInt32());
    }

    [Fact]
    public void Non_bin_location_omits_inventory_assignments()
    {
        using var document = Serialize([Line(1, 4, preferredBin: 202)], isUsedBin: false);

        var json = document.RootElement.GetProperty("lines")[0].GetRawText();

        Assert.DoesNotContain("inventoryAssignments", json);
        Assert.Equal(4m, document.RootElement.GetProperty("lines")[0].GetProperty("quantity").GetDecimal());
    }

    [Fact]
    public void Payload_serializes_the_documented_shape()
    {
        var payload = TransferOrderIFRestletPayloadDTO.CreateForItemFulfillmentRestlet(
            [Line(1, 6, preferredBin: 201)], TransferOrderId, "B", isUsedBin: true);

        var json = JsonSerializer.Serialize(payload, Options);

        Assert.Contains("\"transferOrderId\":12345", json);
        Assert.Contains("\"shipStatus\":\"B\"", json);
        Assert.Contains("\"lines\":[", json);
        Assert.Contains("\"inventoryAssignments\":[", json);
    }

    [Fact]
    public void Lines_are_ordered_by_orderLine()
    {
        using var document = Serialize([Line(9, 1, preferredBin: 201), Line(2, 1, preferredBin: 201)]);

        var ids = document.RootElement.GetProperty("lines")
            .EnumerateArray()
            .Select(x => x.GetProperty("orderLine").GetInt32())
            .ToList();

        Assert.Equal([2, 9], ids);
    }
}
