using System.Globalization;
using System.Text.Json;
using Application.DataTransferObjects.Transactions.Receiving;
using Mapster;
using Microsoft.Extensions.Logging;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions;

public partial class ReceivingIntegration
{
    // Public nested query projections allow the existing typed NetSuite fake to
    // exercise the same mapping/validation as production, without mocking it away.
    public sealed class OperationalDocumentIdentity
    {
        public int Id { get; set; }
    }

    public sealed class OperationalTransferLink
    {
        public int FulfillmentId { get; set; }
        public int SourceId { get; set; }
        public int ShippingLine { get; set; }
        public int? OperationalLine { get; set; }
        public int ReceivingLine { get; set; }
        public int ItemId { get; set; }
        public int UnitId { get; set; }
    }

    public sealed class OperationalReceiptLink
    {
        public int FulfillmentId { get; set; }
        public int ReceiptId { get; set; }
    }

    private sealed record PickerReceiptLinks(
        IReadOnlyDictionary<int, List<OperationalReceiptLink>> ByFulfillment,
        IReadOnlyDictionary<int, List<OperationalReceiptLink>> OwnersByReceipt);

    private sealed record PickerSourceMetadata(
        IReadOnlyDictionary<int, List<OperationalTransferLink>> MappingsByFulfillment,
        IReadOnlyDictionary<int, List<ItemReceiptLineDTO>> MetadataByLine);

    // Snapshot only for this picker load. The per-IF and posting paths still read
    // the source afresh. Reject incomplete SuiteQL sets instead of using a subset.
    private async Task<PickerSourceMetadata> ReadPickerSourceMetadataAsync(int sourceId, int[] fulfillmentIds)
    {
        var query = builderFactory.Create().Distinct().Select(
                ("link.nextdoc", "FulfillmentId"), ("link.previousdoc", "SourceId"),
                ("link.previousline", "ShippingLine"), ("link.nextline", "OperationalLine"),
                ("receiving.id", "ReceivingLine"), ("receiving.item", "ItemId"),
                ("receiving.units", "UnitId"))
            .From("previoustransactionlinelink link")
            .Join("transactionline shipping", "shipping.transaction = link.previousdoc AND shipping.id = link.previousline AND shipping.transactionlinetype = 'SHIPPING'")
            .Join("transactionline receiving", "receiving.transaction = shipping.transaction AND receiving.transferorderitemlineid = shipping.transferorderitemlineid AND receiving.transactionlinetype = 'RECEIVING' AND receiving.item = shipping.item")
            .WithFilters(In("link.nextdoc", fulfillmentIds), Equal("link.previousdoc", sourceId),
                Equal("link.previoustype", "TrnfrOrd"), Equal("link.nexttype", "ItemShip"), Equal("link.linktype", "ShipRcpt"))
            .Build();
        var mappings = await ReadCompleteAsync<OperationalTransferLink>(query.Query);
        if (mappings.Any(map => !fulfillmentIds.Contains(map.FulfillmentId) || map.SourceId != sourceId || map.ReceivingLine <= 0))
            throw OperationalReadError("picker returned an invalid source line mapping");
        var lineIds = mappings.Select(map => map.ReceivingLine).Distinct().ToArray();
        if (lineIds.Length == 0) throw OperationalReadError("picker has no mapped receiving lines");
        // A single NetSuite SuiteQL page cannot prove completeness above 1,000
        // mapped rows. Fall back to the existing per-IF path in that case.
        if (lineIds.Length > 1000) throw OperationalReadError("picker mapping exceeds one metadata page");
        var sourceQuery = builderFactory.Create().Select(("t.tranid", "TranId"))
            .From("transaction t").WithFilters(Equal("t.id", sourceId)).Build();
        var source = await ReadCompleteAsync<OperationalSourceDocument>(sourceQuery.Query);
        if (source.Count != 1 || string.IsNullOrWhiteSpace(source[0].TranId))
            throw OperationalReadError("source order is not uniquely identified");
        var metadata = (await GetItemReceiptLinesAsync(source[0].TranId, transferorder: true, lineIds)).ToList();
        if (metadata.Any(row => !lineIds.Contains(row.LineNumber)))
            throw OperationalReadError("picker returned unexpected receiving-line metadata");
        return new(mappings.GroupBy(map => map.FulfillmentId).ToDictionary(group => group.Key, group => group.ToList()),
            metadata.GroupBy(row => row.LineNumber).ToDictionary(group => group.Key, group => group.ToList()));
    }

    // Only discovery shares these immutable-per-request link snapshots. Selection
    // and posting use fresh native links and REST receipt lines as before.
    private async Task<PickerReceiptLinks> ReadPickerReceiptLinksAsync(int[] fulfillmentIds)
    {
        var linksQuery = builderFactory.Create().Distinct().Select(
                ("link.previousdoc", "FulfillmentId"), ("link.nextdoc", "ReceiptId"))
            .From("nexttransactionlinelink link")
            .WithFilters(In("link.previousdoc", fulfillmentIds), Equal("link.previoustype", "ItemShip"),
                Equal("link.nexttype", "ItemRcpt"), Equal("link.linktype", "TOrdCost")).Build();
        var links = await ReadCompleteAsync<OperationalReceiptLink>(linksQuery.Query);
        if (links.Any(link => !fulfillmentIds.Contains(link.FulfillmentId) || link.ReceiptId <= 0))
            throw OperationalReadError("picker returned an invalid fulfillment/receipt link");
        var receiptIds = links.Select(link => link.ReceiptId).Distinct().ToArray();
        List<OperationalReceiptLink> owners = [];
        if (receiptIds.Length > 0)
        {
            var ownersQuery = builderFactory.Create().Distinct().Select(
                    ("link.previousdoc", "FulfillmentId"), ("link.nextdoc", "ReceiptId"))
                .From("nexttransactionlinelink link")
                .WithFilters(In("link.nextdoc", receiptIds), Equal("link.previoustype", "ItemShip"),
                    Equal("link.nexttype", "ItemRcpt"), Equal("link.linktype", "TOrdCost")).Build();
            owners = await ReadCompleteAsync<OperationalReceiptLink>(ownersQuery.Query);
        }
        return new(links.GroupBy(link => link.FulfillmentId).ToDictionary(group => group.Key, group => group.ToList()),
            owners.GroupBy(link => link.ReceiptId).ToDictionary(group => group.Key, group => group.ToList()));
    }

    private sealed record OperationalItem(int Line, int OrderLine, int SourceId, int ItemId, int UnitId, decimal Quantity);

    private async Task<List<T>> ReadCompleteAsync<T>(string query)
    {
        var response = await netsuiteService.ExecuteSuiteQLQuery<T>(query, limit: 1000);
        if (response.items is null || response.hasMore || response.offset != 0 ||
            response.totalResults > response.items.Count || response.links?.Any(link => link.rel == "next") == true)
            throw OperationalReadError("incomplete SuiteQL results");
        return response.items;
    }

    private static InvalidOperationException OperationalReadError(string reason) => new(
        $"Could not verify operational receiving lines: {reason}. Reload and retry; contact support if this persists.");

    private async Task<List<OperationalItem>> ReadOperationalItemsAsync(string recordType, int id)
    {
        var record = await netsuiteService.MakeRequest<JsonElement>(
            $"{netsuiteService.GetRestAPIURI}/record/v1/{recordType}/{id}?expandSubResources=true", null, HttpMethod.Get);
        if (record.ValueKind != JsonValueKind.Object || ReadInteger(record, "id") != id ||
            !record.TryGetProperty("item", out var sublist) || sublist.ValueKind != JsonValueKind.Object ||
            !sublist.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw OperationalReadError($"{recordType} {id} has no expanded item sublist");

        // Expanded sublists may omit pagination metadata. Any supplied metadata
        // must describe a complete first page; never silently accept a partial list.
        if ((sublist.TryGetProperty("hasMore", out var more) && more.ValueKind != JsonValueKind.False) ||
            (sublist.TryGetProperty("offset", out _) && ReadInteger(sublist, "offset") != 0) ||
            (sublist.TryGetProperty("totalResults", out _) && ReadInteger(sublist, "totalResults") != items.GetArrayLength()) ||
            (sublist.TryGetProperty("count", out _) && ReadInteger(sublist, "count") != items.GetArrayLength()) ||
            (sublist.TryGetProperty("links", out var links) &&
                (links.ValueKind != JsonValueKind.Array || links.EnumerateArray().Any(link =>
                    link.TryGetProperty("rel", out var rel) && rel.GetString() == "next"))))
            throw OperationalReadError($"{recordType} {id} item pagination is incomplete");

        var sourceId = ReadReference(record, "createdFrom");
        if (sourceId is null or <= 0)
            throw OperationalReadError($"{recordType} {id}: createdFrom.id is missing or invalid");
        List<OperationalItem> result = [];
        var itemIndex = 0;
        foreach (var item in items.EnumerateArray())
        {
            var line = ReadInteger(item, "line");
            var orderLine = ReadInteger(item, "orderLine");
            var itemId = ReadReference(item, "item");
            var unitId = ReadIdOrReference(item, "units");
            // NetSuite may return orderDoc as a REST reference ({ id, links, refName })
            // or as a scalar ID. Both must still agree with the fulfillment source.
            var orderDoc = item.TryGetProperty("orderDoc", out _)
                ? ReadIdOrReference(item, "orderDoc")
                : sourceId;
            var quantity = ReadDecimal(item, "quantity");
            var rowContext = $"{recordType} {id} item row {itemIndex}";
            if (line is null or < 0) throw OperationalReadError($"{rowContext}: line is missing or invalid");
            if (orderLine is null or <= 0) throw OperationalReadError($"{rowContext} (line {line}): orderLine is missing or invalid");
            if (orderDoc != sourceId) throw OperationalReadError($"{rowContext} (line {line}): orderDoc does not match createdFrom");
            if (itemId is null or <= 0) throw OperationalReadError($"{rowContext} (line {line}): item.id is missing or invalid");
            if (unitId is null or <= 0) throw OperationalReadError($"{rowContext} (line {line}): units.id is missing or invalid");
            if (quantity is null or <= 0) throw OperationalReadError($"{rowContext} (line {line}): quantity is missing or invalid");
            result.Add(new(line.Value, orderLine.Value, sourceId.Value, itemId.Value, unitId.Value, quantity.Value));
            itemIndex++;
        }
        if (result.Count == 0 || result.GroupBy(line => line.Line).Any(group => group.Count() != 1))
            throw OperationalReadError($"{recordType} {id} has empty or duplicate operational lines");
        return result;
    }

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        return value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(),
            NumberStyles.Number, CultureInfo.InvariantCulture, out number) ? number : null;
    }

    private static int? ReadInteger(JsonElement element, string name)
    {
        var value = ReadDecimal(element, name);
        return value.HasValue && value >= int.MinValue && value <= int.MaxValue && decimal.Truncate(value.Value) == value
            ? (int)value.Value : null;
    }

    private static int? ReadReference(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var reference)
            ? ReadInteger(reference, "id") : null;

    private static int? ReadIdOrReference(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind == JsonValueKind.Object
            ? ReadInteger(value, "id")
            : ReadInteger(value);
    }

    private static int? ReadInteger(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out number) ? number : null;
    }

    private async Task<List<ItemReceiptLineDTO>> ReadOperationalFulfillmentAsync(
        int fulfillmentId, int? expectedSourceId = null, PickerReceiptLinks? pickerLinks = null,
        PickerSourceMetadata? pickerSource = null)
    {
        var stageTimer = System.Diagnostics.Stopwatch.StartNew();
        var operational = await ReadOperationalItemsAsync("itemFulfillment", fulfillmentId);
        var operationalMs = stageTimer.ElapsedMilliseconds;
        var sourceId = operational[0].SourceId;
        if (expectedSourceId.HasValue && sourceId != expectedSourceId.Value)
            throw OperationalReadError("the selected fulfillment belongs to a different source order");
        var mappingQuery = builderFactory.Create().Distinct().Select(
                ("link.previousdoc", "SourceId"), ("link.previousline", "ShippingLine"),
                ("link.nextline", "OperationalLine"), ("receiving.id", "ReceivingLine"),
                ("receiving.item", "ItemId"), ("receiving.units", "UnitId"))
            .From("previoustransactionlinelink link")
            .Join("transactionline shipping", "shipping.transaction = link.previousdoc AND shipping.id = link.previousline AND shipping.transactionlinetype = 'SHIPPING'")
            .Join("transactionline receiving", "receiving.transaction = shipping.transaction AND receiving.transferorderitemlineid = shipping.transferorderitemlineid AND receiving.transactionlinetype = 'RECEIVING' AND receiving.item = shipping.item")
            .WithFilters(Equal("link.nextdoc", fulfillmentId), Equal("link.previousdoc", sourceId),
                Equal("link.previoustype", "TrnfrOrd"), Equal("link.nexttype", "ItemShip"), Equal("link.linktype", "ShipRcpt"))
            .Build();
        var mappings = pickerSource is null
            ? await ReadCompleteAsync<OperationalTransferLink>(mappingQuery.Query)
            : pickerSource.MappingsByFulfillment.GetValueOrDefault(fulfillmentId) ?? [];
        var mappingMs = stageTimer.ElapsedMilliseconds - operationalMs;
        if (mappings.Count != operational.Count)
            throw OperationalReadError("expanded fulfillment and native link line sets differ");
        // UI metadata comes from the TO RECEIVING line, including its destination
        // location and UoM. IF accounting rows (including null units) are never read.
        List<ItemReceiptLineDTO> metadata;
        if (pickerSource is null)
        {
            var sourceQuery = builderFactory.Create().Select(("t.tranid", "TranId"))
                .From("transaction t").WithFilters(Equal("t.id", sourceId)).Build();
            var source = await ReadCompleteAsync<OperationalSourceDocument>(sourceQuery.Query);
            if (source.Count != 1 || string.IsNullOrWhiteSpace(source[0].TranId))
                throw OperationalReadError("source order is not uniquely identified");
            metadata = (await GetItemReceiptLinesAsync(source[0].TranId, transferorder: true,
                mappings.Select(map => map.ReceivingLine).Distinct().ToArray())).ToList();
        }
        else
        {
            metadata = mappings.SelectMany(map => pickerSource.MetadataByLine.GetValueOrDefault(map.ReceivingLine) ?? [])
                .Distinct().ToList();
        }
        var metadataMs = stageTimer.ElapsedMilliseconds - operationalMs - mappingMs;
        List<ItemReceiptLineDTO> result = [];
        foreach (var line in operational)
        {
            var matches = mappings.Where(map => map.SourceId == line.SourceId && map.ShippingLine == line.OrderLine &&
                map.OperationalLine == line.Line).ToArray();
            if (matches.Length != 1 || matches[0].ReceivingLine <= 0 ||
                matches[0].ItemId != line.ItemId || matches[0].UnitId != line.UnitId)
                throw OperationalReadError($"IF {fulfillmentId} line {line.Line} has no unique native receiving-line mapping");
            var rows = metadata.Where(row => row.LineNumber == matches[0].ReceivingLine).ToArray();
            if (rows.Length != 1 || rows[0].ItemId != line.ItemId || rows[0].UoMRate <= 0)
                throw OperationalReadError("source receiving-line metadata is missing or ambiguous");
            // The same TO receiving line can be shared across IFs. Never mutate
            // a DTO held by the request-scoped metadata snapshot.
            var row = pickerSource is null ? rows[0] : rows[0].Adapt<ItemReceiptLineDTO>();
            row.QuantityFulfilled = line.Quantity;
            row.QuantityReceived = 0;
            result.Add(row);
        }
        if (result.GroupBy(row => row.LineNumber).Any(group => group.Count() != 1))
            throw OperationalReadError("multiple IF lines map to the same receiving line");

        var receiptQuery = builderFactory.Create().Distinct().Select(
                ("link.previousdoc", "FulfillmentId"), ("link.nextdoc", "ReceiptId"))
            .From("nexttransactionlinelink link")
            .WithFilters(Equal("link.previousdoc", fulfillmentId), Equal("link.previoustype", "ItemShip"),
                Equal("link.nexttype", "ItemRcpt"), Equal("link.linktype", "TOrdCost")).Build();
        var receipts = pickerLinks is null
            ? await ReadCompleteAsync<OperationalReceiptLink>(receiptQuery.Query)
            : pickerLinks.ByFulfillment.GetValueOrDefault(fulfillmentId) ?? [];
        foreach (var receiptId in receipts.Select(link => link.ReceiptId).Distinct())
        {
            if (receiptId <= 0) throw OperationalReadError("invalid receipt identity");
            var ownersQuery = builderFactory.Create().Distinct().Select(
                    ("link.previousdoc", "FulfillmentId"), ("link.nextdoc", "ReceiptId"))
                .From("nexttransactionlinelink link")
                .WithFilters(Equal("link.nextdoc", receiptId), Equal("link.previoustype", "ItemShip"),
                    Equal("link.nexttype", "ItemRcpt"), Equal("link.linktype", "TOrdCost")).Build();
            var owners = pickerLinks is null
                ? await ReadCompleteAsync<OperationalReceiptLink>(ownersQuery.Query)
                : pickerLinks.OwnersByReceipt.GetValueOrDefault(receiptId) ?? [];
            if (owners.Count == 0 || owners.Any(link => link.FulfillmentId != fulfillmentId || link.ReceiptId != receiptId))
                throw OperationalReadError($"receipt {receiptId} cannot be attributed to one fulfillment");
            var nativeReceiptQuery = builderFactory.Create().Distinct().Select(
                    ("link.previousdoc", "SourceId"), ("link.previousline", "ReceivingLine"),
                    ("link.nextline", "OperationalLine"))
                .From("previoustransactionlinelink link")
                .WithFilters(Equal("link.nextdoc", receiptId), Equal("link.previoustype", "TrnfrOrd"),
                    Equal("link.nexttype", "ItemRcpt"), Equal("link.linktype", "ShipRcpt")).Build();
            var nativeReceiptLinks = await ReadCompleteAsync<OperationalReceiptLineLink>(nativeReceiptQuery.Query);
            var receiptItems = await ReadOperationalItemsAsync("itemReceipt", receiptId);
            if (nativeReceiptLinks.Count != receiptItems.Count)
                throw OperationalReadError($"receipt {receiptId} expanded and native line sets differ");
            foreach (var received in receiptItems)
            {
                if (nativeReceiptLinks.Count(link => link.SourceId == received.SourceId &&
                    link.ReceivingLine == received.OrderLine && link.OperationalLine == received.Line) != 1)
                    throw OperationalReadError($"receipt {receiptId} line {received.Line} has no unique native link");
                var matches = mappings.Where(map => map.SourceId == received.SourceId && map.ReceivingLine == received.OrderLine &&
                    map.ItemId == received.ItemId && map.UnitId == received.UnitId).ToArray();
                if (matches.Length != 1) throw OperationalReadError($"receipt {receiptId} line {received.Line} is ambiguous");
                var row = result.SingleOrDefault(row => row.LineNumber == matches[0].ReceivingLine);
                if (row is null) throw OperationalReadError($"receipt {receiptId} does not belong to the selected fulfillment lines");
                row.QuantityReceived += received.Quantity;
            }
        }
        if (expectedSourceId.HasValue)
            logger.LogInformation(
                "Receiving picker verified IF {ItemFulfillmentId} on TO {TransferOrderId}: operational REST {OperationalMs}ms, native mapping {MappingMs}ms, source/metadata {MetadataMs}ms, receipt verification {ReceiptMs}ms, {ReceiptCount} receipt(s), {LineCount} line(s).",
                fulfillmentId, sourceId, operationalMs, mappingMs, metadataMs,
                stageTimer.ElapsedMilliseconds - operationalMs - mappingMs - metadataMs,
                receipts.Select(link => link.ReceiptId).Distinct().Count(), result.Count);
        return result;
    }

    public sealed class OperationalSourceDocument
    {
        public string TranId { get; set; } = string.Empty;
    }

    public sealed class OperationalReceiptLineLink
    {
        public int SourceId { get; set; }
        public int ReceivingLine { get; set; }
        public int? OperationalLine { get; set; }
    }
}
