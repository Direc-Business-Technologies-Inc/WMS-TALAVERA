using System.Text.Json;
using System.Text.Json.Nodes;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Integration.NS.Implementations.Transactions.ReceivingIntegration;

namespace Integration.NS.Tests.Implementations.Transactions;

public sealed class ReceivingOperationalLinesTests
{
    internal sealed class Fixture
    {
        public RestCapturingNetSuiteClient Client { get; } = new();
        public ReceivingIntegration Sut { get; }
        public JsonObject Fulfillment { get; } = Record(456, 299, 3);
        public Dictionary<int, JsonObject> Receipts { get; } = [];
        public List<OperationalTransferLink> Mappings { get; } =
            [new() { SourceId = 208281, ShippingLine = 299, OperationalLine = 0,
                ReceivingLine = 300, ItemId = 49312, UnitId = 1 }];
        public List<TransferOrderLineRemaining> Ledger { get; } =
            [new() { LineId = 300, ItemCode = "ITEM-49312", QuantityPlanned = 10 }];
        public bool Incomplete { get; set; }
        public decimal ConversionRate { get; set; } = 1;

        public Fixture()
        {
            Client.QueryItems.Add(new OperationalDocumentIdentity { Id = 456 });
            Client.QueryItems.Add(new OperationalSourceDocument { TranId = "TO208281" });
            Client.OnRead = url => JsonSerializer.SerializeToElement(url.Contains("itemFulfillment")
                ? Fulfillment : Receipts.Single(pair => url.Contains($"/{pair.Key}?")).Value);
            Client.OnQuery = (query, type) =>
            {
                if (type == typeof(OperationalTransferLink)) return Response(Mappings);
                if (type == typeof(TransferOrderLineRemaining)) return Response(Ledger);
                if (type == typeof(ItemReceiptLineDTO)) return Response(Mappings.Select(map => new ItemReceiptLineDTO
                {
                    LineNumber = map.ReceivingLine, ItemId = map.ItemId, ItemCode = "ITEM-49312",
                    UoM = "PC", UoMRate = ConversionRate, QuantityPlanned = 10
                }).DistinctBy(row => row.LineNumber).ToList());
                if (type == typeof(OperationalReceiptLink)) return Response(Receipts.Keys
                    .Where(id => !query.Contains("link.nextdoc =") || query.Contains($"link.nextdoc = {id}"))
                    .SelectMany(id => new[]
                {
                    new OperationalReceiptLink { FulfillmentId = 456, ReceiptId = id },
                    new OperationalReceiptLink { FulfillmentId = 456, ReceiptId = id }
                }).ToList()); // deliberately duplicated native links
                if (type == typeof(OperationalReceiptLineLink)) return Response(ReceiptItems(query));
                return null;
            };
            Sut = new(Client, new HttpContextAccessor(), new SuiteQLQueryBuilderFactoryService(),
                NullLogger<ReceivingIntegration>.Instance);
        }

        private List<OperationalReceiptLineLink> ReceiptItems(string query) => Receipts
            .Where(pair => query.Contains($"link.nextdoc = {pair.Key}"))
            .SelectMany(pair => pair.Value["item"]!["items"]!.AsArray().Select(line => new OperationalReceiptLineLink
            { SourceId = 208281, ReceivingLine = (int)line!["orderLine"]!, OperationalLine = (int)line["line"]! })).ToList();

        private NetSuiteResponse<T> Response<T>(List<T> rows) => new()
        { items = rows, totalResults = rows.Count, hasMore = Incomplete };

        public static JsonObject Record(int id, int orderLine, decimal quantity) => JsonNode.Parse($$$"""
            {"id":"{{{id}}}","createdFrom":{"id":"208281"},"item":{"hasMore":false,"offset":0,"totalResults":1,"items":[
              {"line":0,"orderLine":{{{orderLine}}},"orderDoc":208281,"item":{"id":"49312"},"units":{"id":"1"},"quantity":{{{quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}}
            ]}}
            """)!.AsObject();

        public ItemReceiptDTO Receipt(decimal quantity) => new()
        {
            SourceType = ItemReceiptDTO.SourceTypes.TransferOrder, SourceInternalId = 208281, ItemFulfillmentId = 456,
            Lines = [new() { LineNumber = 300, ItemId = 49312, ItemCode = "ITEM-49312", QuantityPlanned = 10,
                QuantityAlloted = quantity, InventoryDetails = [new() { Status = new() { Id = 1 }, QuantityAlloted = quantity }] }]
        };
    }

    [Fact]
    public async Task Operational_zero_maps_299_to_300_without_reading_accounting_rows_1_and_2_or_null_units()
    {
        var fixture = new Fixture();
        // If accounting data were consulted it would provide two rows with null
        // units (+3 INTRANSIT / -3 blank type), not the operational line zero.
        var rows = (await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512")).ToArray();
        var row = Assert.Single(rows);
        Assert.Equal(300, row.LineNumber);
        Assert.Equal(3m, row.QuantityFulfilled);
        Assert.Equal(3m, row.QuantityOpen);
        Assert.Equal("PC", row.UoM);
        Assert.DoesNotContain(fixture.Client.Queries, query => query.Contains("transactionline ifline"));
        Assert.Contains(fixture.Client.Queries, query => query.Contains("receiving.transferorderitemlineid = shipping.transferorderitemlineid"));
        Assert.Contains("expandSubResources=true", Assert.Single(fixture.Client.Reads));
        Assert.True(await fixture.Sut.PostItemReceipt(fixture.Receipt(3)));
        using var body = JsonDocument.Parse(Assert.Single(fixture.Client.Writes).Body!);
        Assert.Equal(300, body.RootElement.GetProperty("lines")[0].GetProperty("orderLine").GetInt32());
    }

    [Theory]
    [InlineData("{\"id\":\"208281\",\"links\":[],\"refName\":\"TO208281\"}")]
    [InlineData("208281")]
    [InlineData("\"208281\"")]
    public async Task Matching_orderDoc_reference_or_scalar_allows_verified_receipt(string orderDocJson)
    {
        var fixture = new Fixture();
        fixture.Fulfillment["item"]!["items"]![0]!["orderDoc"] = JsonNode.Parse(orderDocJson);

        var row = Assert.Single(await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512"));
        Assert.Equal(300, row.LineNumber);
        Assert.True(await fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Single(fixture.Client.Writes);
    }

    [Theory]
    [InlineData("{\"id\":\"999\"}")]
    [InlineData("{\"links\":[]}")]
    [InlineData("{\"id\":\"invalid\"}")]
    [InlineData("null")]
    [InlineData("999")]
    public async Task Invalid_or_different_orderDoc_reference_fails_before_receipt_write(string orderDocJson)
    {
        var fixture = new Fixture();
        fixture.Fulfillment["item"]!["items"]![0]!["orderDoc"] = JsonNode.Parse(orderDocJson);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512"));
        Assert.Contains("orderDoc does not match createdFrom", error.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Theory]
    [InlineData("\"134\"")]
    [InlineData("134")]
    [InlineData("{\"id\":\"134\",\"links\":[],\"refName\":\"PC\"}")]
    public async Task Matching_units_scalar_or_reference_maps_and_posts(string unitsJson)
    {
        var fixture = new Fixture();
        fixture.Mappings[0].UnitId = 134;
        fixture.Fulfillment["item"]!["items"]![0]!["units"] = JsonNode.Parse(unitsJson);

        var row = Assert.Single(await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512"));
        Assert.Equal(300, row.LineNumber);
        Assert.True(await fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Single(fixture.Client.Writes);
    }

    [Fact]
    public async Task Different_scalar_unit_id_fails_mapping_before_receipt_write()
    {
        var fixture = new Fixture();
        fixture.Fulfillment["item"]!["items"]![0]!["units"] = JsonValue.Create("134");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512"));
        Assert.Contains("no unique native receiving-line mapping", error.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Theory]
    [InlineData("line")]
    [InlineData("orderLine")]
    [InlineData("quantity")]
    [InlineData("units")]
    [InlineData("item")]
    public async Task Missing_operational_fields_fail_closed_for_UI_and_fresh_post(string field)
    {
        var fixture = new Fixture();
        fixture.Fulfillment["item"]!["items"]![0]!.AsObject().Remove(field);
        var uiError = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512"));
        Assert.Contains(field switch
        {
            "item" => "item.id",
            "units" => "units.id",
            _ => field
        }, uiError.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Theory]
    [InlineData("hasMore", "true")]
    [InlineData("offset", "1")]
    [InlineData("totalResults", "2")]
    [InlineData("links", "[{\"rel\":\"next\",\"href\":\"ignored\"}]")]
    public async Task Partial_REST_sublists_are_rejected(string field, string value)
    {
        var fixture = new Fixture();
        fixture.Fulfillment["item"]![field] = JsonNode.Parse(value);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Fact]
    public async Task Partial_SuiteQL_results_are_rejected()
    {
        var fixture = new Fixture { Incomplete = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Fact]
    public async Task Operational_quantity_is_already_in_the_verified_unit_and_is_not_divided_twice()
    {
        var fixture = new Fixture { ConversionRate = 12 };
        var row = Assert.Single(await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512"));
        Assert.Equal(3m, row.QuantityFulfilled);
        Assert.Equal(12m, row.UoMRate);
    }

    [Fact]
    public async Task Missing_fresh_TO_ledger_line_fails_before_write()
    {
        var fixture = new Fixture();
        fixture.Ledger.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Fact]
    public async Task REST_line_set_must_match_native_links_even_without_pagination_metadata()
    {
        var fixture = new Fixture();
        fixture.Fulfillment["item"]!.AsObject().Remove("totalResults");
        fixture.Mappings.Add(new() { SourceId = 208281, ShippingLine = 399, OperationalLine = 4,
            ReceivingLine = 400, ItemId = 49312, UnitId = 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Fact]
    public async Task Receipt_shared_by_multiple_fulfillments_is_not_allocated_by_item()
    {
        var fixture = new Fixture();
        fixture.Receipts.Add(701, Fixture.Record(701, 300, 1));
        var original = fixture.Client.OnQuery!;
        fixture.Client.OnQuery = (query, type) => type == typeof(OperationalReceiptLink) && query.Contains("link.nextdoc =")
            ? new NetSuiteResponse<OperationalReceiptLink> { items = [new() { FulfillmentId = 999, ReceiptId = 701 }], totalResults = 1 }
            : original(query, type);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Theory]
    [InlineData("ambiguous")]
    [InlineData("missing")]
    [InlineData("unit")]
    [InlineData("source")]
    [InlineData("offset-guess")]
    public async Task Native_identity_mismatches_and_ambiguities_are_rejected(string scenario)
    {
        var fixture = new Fixture();
        switch (scenario)
        {
            case "ambiguous": fixture.Mappings.Add(new() { SourceId = 208281, ShippingLine = 299,
                OperationalLine = 0, ReceivingLine = 400, ItemId = 49312, UnitId = 1 }); break;
            case "missing": fixture.Mappings.Clear(); break;
            case "unit": fixture.Mappings[0].UnitId = 2; break;
            case "source": fixture.Fulfillment["createdFrom"]!["id"] = "999"; break;
            case "offset-guess": fixture.Mappings[0].OperationalLine = 1; break;
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
        Assert.Empty(fixture.Client.Writes);
    }

    [Fact]
    public async Task Equal_quantity_receipts_count_once_each_despite_duplicate_native_links()
    {
        var fixture = new Fixture();
        fixture.Receipts.Add(701, Fixture.Record(701, 300, 1));
        fixture.Receipts.Add(702, Fixture.Record(702, 300, 1));
        var row = Assert.Single(await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512"));
        Assert.Equal(2m, row.QuantityReceived);
        Assert.Equal(1m, row.QuantityOpen);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(2)));
        Assert.Empty(fixture.Client.Writes);
        Assert.True(await fixture.Sut.PostItemReceipt(fixture.Receipt(1)));
    }

    [Fact]
    public async Task Repeated_items_keep_their_operational_and_receiving_line_identity()
    {
        var fixture = new Fixture();
        var second = fixture.Fulfillment["item"]!["items"]![0]!.DeepClone();
        second["line"] = 4; second["orderLine"] = 399; second["quantity"] = 2;
        fixture.Fulfillment["item"]!["items"]!.AsArray().Add(second);
        fixture.Fulfillment["item"]!["totalResults"] = 2;
        fixture.Mappings.Add(new() { SourceId = 208281, ShippingLine = 399, OperationalLine = 4,
            ReceivingLine = 400, ItemId = 49312, UnitId = 1 });
        fixture.Receipts.Add(701, Fixture.Record(701, 400, 1));
        var rows = (await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512")).ToArray();
        Assert.Equal(3m, rows.Single(row => row.LineNumber == 300).QuantityOpen);
        Assert.Equal(1m, rows.Single(row => row.LineNumber == 400).QuantityOpen);
    }

    [Theory]
    [InlineData(4, 0, "remaining quantity 3")]
    [InlineData(3, 8, "exceeds remaining 2")]
    public async Task Fresh_IF_and_TO_ledger_ceilings_both_apply(decimal quantity, decimal ledgerReceived, string message)
    {
        var fixture = new Fixture();
        fixture.Ledger[0] = fixture.Ledger[0] with { QuantityReceived = ledgerReceived };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Sut.PostItemReceipt(fixture.Receipt(quantity)));
        Assert.Contains(message, error.Message);
        Assert.Empty(fixture.Client.Writes);
    }
}
