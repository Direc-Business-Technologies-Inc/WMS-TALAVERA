using System.Text.Json;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Pins <see cref="NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform"/>.
///
/// Background: every earlier line-set guard measured the payload against the SuiteQL
/// open-line read, so a line that read called open but the
/// <c>!transform/itemFulfillment</c> preview did not expose passed every guard and then
/// failed the whole POST with "You have attempted an invalid sublist or line item
/// operation" (field report 2026-10-23, order 226006: <c>extra=0</c> while NetSuite
/// rejected 1,022 entries). The verification now measures against the preview.
/// </summary>
public class NetSuiteApiClientServiceTOFulfillmentTransformVerificationTests
{
    private const int OrderId = 226006;

    private static PostTransferOrderDTO Line(
        int lineId,
        decimal scannedQuantity,
        bool isBad = false)
        => new()
        {
            NetsuiteOrderInternalId = OrderId,
            LineSequenceNumber = lineId,
            MaterialCode = $"ITEM{lineId}",
            MaterialName = $"Item {lineId}",
            ScannedQuantity = scannedQuantity,
            IsBad = isBad,
            IsLocationUsedBin = true,
            NetsuiteMaterialPrefferedBinId = 777
        };

    private static NetSuiteApiClientService.TOFulfillmentTransformLine TransformLine(
        int? orderLine,
        decimal? quantity = 1,
        bool hasAssignment = false,
        int? line = null)
        => new()
        {
            OrderLine = orderLine,
            Line = line,
            Quantity = quantity,
            InventoryDetail = hasAssignment
                ? JsonDocument.Parse("""
                    {"inventoryAssignment":{"items":[{"quantity":1,"binNumber":{"id":"777"}}]}}
                    """).RootElement
                : null
        };

    [Fact]
    public void Every_exposed_line_survives_unchanged()
    {
        PostTransferOrderDTO[] lines =
        [
            Line(10, 2),
            Line(20, 0)
        ];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10, quantity: 2),
            TransformLine(20)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 2);

        Assert.True(result.Available);
        Assert.Null(result.BlockingError);
        Assert.Empty(result.RejectedScans);
        Assert.Empty(result.DroppedFillerIds);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(2, result.TransformLineCount);
    }

    [Fact]
    public void Filler_the_preview_does_not_expose_is_dropped()
    {
        // The 2026-10-23 field failure in miniature: the SuiteQL read says line 20 is
        // open, the transform does not expose it. Addressing it fails the whole save.
        PostTransferOrderDTO[] lines =
        [
            Line(10, 2),
            Line(20, 0)
        ];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10, quantity: 2),
            TransformLine(30) // a line the read did not report, keeps the count honest
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 2);

        Assert.True(result.Available);
        Assert.Null(result.BlockingError);
        Assert.Equal([20], result.DroppedFillerIds);
        Assert.Single(result.Lines);
        Assert.Equal(10, result.Lines[0].LineSequenceNumber);
    }

    [Fact]
    public void Scan_the_preview_does_not_expose_is_rejected_by_name()
    {
        PostTransferOrderDTO[] lines =
        [
            Line(10, 5),
            Line(20, 0)
        ];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(20),
            TransformLine(30)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 2);

        Assert.True(result.Available);
        Assert.Single(result.RejectedScans);
        Assert.Contains("WMS line 10", result.RejectedScans[0]);
        Assert.Contains("ITEM10", result.RejectedScans[0]);
        // A rejected scan must never be silently dropped - only fillers are droppable.
        Assert.Empty(result.DroppedFillerIds);
    }

    [Fact]
    public void Preview_smaller_than_the_open_set_blocks_instead_of_dropping()
    {
        // Dropping these would let the transform auto-fulfil them at their pre-filled
        // quantity, so the save must stop and say so.
        PostTransferOrderDTO[] lines =
        [
            Line(10, 0),
            Line(20, 0),
            Line(30, 0)
        ];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 3);

        Assert.True(result.Available);
        Assert.NotNull(result.BlockingError);
        Assert.Contains("exposes 1 line", result.BlockingError);
        Assert.Contains("expects 3", result.BlockingError);
        Assert.Contains("20", result.BlockingError);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public void Preview_without_orderLine_ids_fails_open_instead_of_guessing()
    {
        // `line` is a sublist POSITION, never an id. A preview that carries only
        // `line` must not be turned into ids by assumption.
        PostTransferOrderDTO[] lines = [Line(10, 1)];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(orderLine: null, line: 1),
            TransformLine(orderLine: null, line: 2)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 1);

        Assert.False(result.Available);
        Assert.Contains("orderLine", result.UnavailableReason);
    }

    [Fact]
    public void Empty_preview_fails_open()
    {
        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(
            [Line(10, 1)], [], 1);

        Assert.False(result.Available);
    }

    [Fact]
    public void Filler_with_a_prefilled_assignment_is_reported()
    {
        // An empty ?replace= on such a line removes sublist lines - the literal wording of
        // the 2026-10-23 error. Reported (not refused) so the diag names the offenders.
        PostTransferOrderDTO[] lines =
        [
            Line(10, 2),
            Line(20, 0)
        ];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10, quantity: 2),
            TransformLine(20, hasAssignment: true)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 2);

        Assert.True(result.Available);
        Assert.Equal([20], result.FillersWithInventoryDetail);
        Assert.Empty(result.DroppedFillerIds);
        Assert.Equal(2, result.Lines.Count);
    }

    [Fact]
    public void Scan_beyond_the_preview_quantity_is_rejected_by_name()
    {
        // The preview's quantity is the amount NetSuite would fulfil if the line
        // went unaddressed - the authoritative cap, in the POST's quantity unit.
        PostTransferOrderDTO[] lines =
        [
            Line(10, 29)
        ];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10, quantity: 23)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 1);

        Assert.True(result.Available);
        Assert.Single(result.RejectedScans);
        Assert.Contains("WMS line 10", result.RejectedScans[0]);
        Assert.Contains("scanned 29", result.RejectedScans[0]);
        Assert.Contains("exposes only 23", result.RejectedScans[0]);
        Assert.Empty(result.Lines);
    }

    [Fact]
    public void Scan_at_the_preview_quantity_is_kept()
    {
        PostTransferOrderDTO[] lines = [Line(10, 23)];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10, quantity: 23)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 1);

        Assert.True(result.Available);
        Assert.Empty(result.RejectedScans);
        Assert.Single(result.Lines);
    }

    [Fact]
    public void Preview_without_a_quantity_does_not_cap_the_scan()
    {
        // quantity is nullable on the preview payload; when absent the SuiteQL
        // packable-quantity guard remains the only cap (fail open on this check).
        PostTransferOrderDTO[] lines = [Line(10, 500)];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10, quantity: null)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 1);

        Assert.True(result.Available);
        Assert.Empty(result.RejectedScans);
        Assert.Single(result.Lines);
    }

    [Fact]
    public void Duplicate_preview_entries_for_one_orderLine_collapse()
    {
        PostTransferOrderDTO[] lines = [Line(10, 1), Line(20, 0)];

        NetSuiteApiClientService.TOFulfillmentTransformLine[] preview =
        [
            TransformLine(10),
            TransformLine(10),
            TransformLine(20)
        ];

        var result = NetSuiteApiClientService.VerifyTOFulfillmentAgainstTransform(lines, preview, 2);

        Assert.True(result.Available);
        Assert.Equal(2, result.TransformLineCount);
        Assert.Equal(2, result.Lines.Count);
    }

    // -----------------------------------------------------------------
    // The preview read failed open in the field (2026-10-23, order 226006:
    // transform=-1, no reason in the diag) because NetSuite returns
    // reference fields as objects, which a strict int? binding rejects.
    // -----------------------------------------------------------------

    private static readonly JsonSerializerOptions LenientOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    [Fact]
    public void Reference_shaped_orderLine_and_string_ids_deserialize()
    {
        const string json = """
            {"orderLine":{"id":"1001","refName":"Line 1"},"quantity":5}
            """;

        var line = JsonSerializer.Deserialize<NetSuiteApiClientService.TOFulfillmentTransformLine>(json, LenientOptions);

        Assert.NotNull(line);
        Assert.Equal(1001, line!.OrderLine);
        Assert.Equal(5m, line.Quantity);
    }

    [Fact]
    public void Numeric_string_and_number_orderLine_deserialize()
    {
        const string stringId = """{"orderLine":"2002","quantity":"3"}""";
        const string numberId = """{"orderLine":3003,"quantity":3}""";

        var fromString = JsonSerializer.Deserialize<NetSuiteApiClientService.TOFulfillmentTransformLine>(stringId, LenientOptions);
        var fromNumber = JsonSerializer.Deserialize<NetSuiteApiClientService.TOFulfillmentTransformLine>(numberId, LenientOptions);

        Assert.Equal(2002, fromString!.OrderLine);
        Assert.Equal(3m, fromString.Quantity);
        Assert.Equal(3003, fromNumber!.OrderLine);
    }

    [Fact]
    public void Missing_orderLine_deserializes_as_null()
    {
        const string json = """{"line":1,"quantity":1}""";

        var line = JsonSerializer.Deserialize<NetSuiteApiClientService.TOFulfillmentTransformLine>(json, LenientOptions);

        Assert.NotNull(line);
        Assert.Null(line!.OrderLine);
        Assert.Equal(1, line.Line);
    }

    // -----------------------------------------------------------------
    // The preview cannot be read at all: NetSuite answers GET on
    // !transform/itemFulfillment with 405 INVALID_METHOD (field report
    // 2026-10-23, order 223191). Without a preview, the only defence left
    // is refusing a payload above the measured transform ceiling.
    // -----------------------------------------------------------------

    [Theory]
    [InlineData(1000)]
    [InlineData(25)]
    public void Payload_at_or_below_the_ceiling_is_allowed(int lineCount)
    {
        NetSuiteApiClientService.EnsureTOFulfillmentTransformSize(lineCount, 1000, "ISTR-TEST");
    }

    [Theory]
    [InlineData(1022)]
    [InlineData(1584)]
    public void Payload_above_the_ceiling_is_refused_with_both_sizes(int lineCount)
    {
        var ex = Assert.Throws<Exception>(
            () => NetSuiteApiClientService.EnsureTOFulfillmentTransformSize(lineCount, 1000, "ISTR-THM00100000837/223191"));

        Assert.Contains(lineCount.ToString("N0"), ex.Message);
        Assert.Contains("1,000", ex.Message);
        Assert.Contains("ISTR-THM00100000837/223191", ex.Message);
        Assert.Contains("WMS_TO_IF_USE_RESTLET", ex.Message);
        Assert.Contains("Nothing was saved", ex.Message);
    }
}
