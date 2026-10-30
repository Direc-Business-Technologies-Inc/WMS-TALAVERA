using System.Globalization;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Commands.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Integration.NS.Services;

namespace Integration.NS.Implementations.Transactions;

/// <summary>Uncached, bounded NetSuite reads for unattended STR fulfillment.</summary>
internal sealed class AutoStrNetSuiteIntegration(INetSuiteApiClientService client) : IAutoStrNetSuiteIntegration
{
    public async Task<IReadOnlyList<AutoStrOrderDTO>> DiscoverApprovedAsync(int approvalStatusId, string approvalTimeField,
        IReadOnlyList<int> subsidiaryIds, DateTime changedSinceUtc, CancellationToken ct)
    {
        ValidateApprovalField(approvalTimeField);
        if (approvalStatusId <= 0 || subsidiaryIds.Count == 0 || subsidiaryIds.Any(id => id <= 0))
            throw new ArgumentException("Verified approval and subsidiary IDs are required.");
        var scope = string.Join(",", subsidiaryIds.Distinct());
        var since = changedSinceUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var query = $"""
            SELECT t.id AS NetsuiteOrderId, t.tranid AS OrderNumber,
                t.custbody_dbti_transfer_category AS TransferCategoryId,
                tl.location AS FromLocationId, t.transferlocation AS ToLocationId,
                t.subsidiary AS SubsidiaryId,
                t.custbody_dbti_custom_approval_status AS ApprovalStatusId,
                TO_CHAR(t.{approvalTimeField}, 'YYYY-MM-DD"T"HH24:MI:SS') AS ApprovedAt
            FROM transaction t
            JOIN transactionline tl ON tl.transaction = t.id AND tl.mainline = 'T'
            WHERE t.recordtype IN ('transferorder', 'intercompanytransferorder')
              AND t.custbody_dbti_custom_approval_status = {approvalStatusId}
              AND t.subsidiary IN ({scope})
              AND t.{approvalTimeField} >= TO_DATE('{since}', 'YYYY-MM-DD HH24:MI:SS')
            ORDER BY t.id
            """;
        var rows = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            (limit, offset) => client.ExecuteSuiteQLQuery<OrderRow>(query, limit, offset, ct),
            1000, 100, "approved STR discovery");
        return rows.Select(Map).ToArray();
    }

    public async Task<AutoStrOrderDTO?> GetCurrentOrderAsync(int orderId, string approvalTimeField, CancellationToken ct)
    {
        ValidateApprovalField(approvalTimeField);
        if (orderId <= 0) throw new ArgumentOutOfRangeException(nameof(orderId));
        var query = $"""
            SELECT t.id AS NetsuiteOrderId, t.tranid AS OrderNumber,
                t.custbody_dbti_transfer_category AS TransferCategoryId,
                tl.location AS FromLocationId, t.transferlocation AS ToLocationId,
                t.subsidiary AS SubsidiaryId,
                t.custbody_dbti_custom_approval_status AS ApprovalStatusId,
                TO_CHAR(t.{approvalTimeField}, 'YYYY-MM-DD"T"HH24:MI:SS') AS ApprovedAt
            FROM transaction t
            JOIN transactionline tl ON tl.transaction = t.id AND tl.mainline = 'T'
            WHERE t.id = {orderId}
              AND t.recordtype IN ('transferorder', 'intercompanytransferorder')
            """;
        var response = await client.ExecuteSuiteQLQuery<OrderRow>(query, 2, 0, ct);
        if (response?.items is null || response.items.Count > 1 || response.totalResults > 1)
            throw new InvalidOperationException("The STR header could not be read uniquely.");
        return response.items.Count == 0 ? null : Map(response.items[0]);
    }

    public async Task<(IReadOnlyList<AutoStrLineDTO> Lines, int OpenCount)> GetCurrentLinesAsync(
        AutoStrOrderDTO order, string? lineStatusField, bool categoryPolicyVerified, CancellationToken ct)
    {
        var status = StrAutoFulfillmentOptions.IsLineStatusField(lineStatusField)
            ? $"tl.{lineStatusField}"
            : categoryPolicyVerified
                ? $"CASE WHEN t.custbody_dbti_transfer_category = 4 THEN {InventoryStatusMap.Bad} ELSE {InventoryStatusMap.Good} END"
                : throw new InvalidOperationException("A verified per-line inventory condition source is required.");
        var orderId = order.NetsuiteOrderId;
        if (orderId <= 0) throw new ArgumentOutOfRangeException(nameof(order));
        // Stock is aggregated separately at the location and at its preferred
        // bin, so a second item row cannot spend the same balance twice.
        var query = $"""
            SELECT tl.id AS LineId, tl.item AS ItemId, tl.location AS LocationId,
                {status} AS InventoryStatusId,
                COALESCE(vendorbin.AssignedBinId, pref.PreferredBinId) AS BinId,
                loc.usebins AS UseBins, ABS(tl.quantity) AS OrderedQuantity,
                ABS(NVL(tl.quantityshiprecv, 0)) AS ShippedQuantity,
                CASE WHEN loc.usebins = 'T' THEN binstock.Available ELSE locstock.Available END AS Available,
                NVL(u.conversionrate, 1) AS UomRate, tl.isclosed AS IsClosed,
                i.itemid AS MaterialCode, i.displayname AS MaterialName
            FROM transactionline tl
            JOIN transaction t ON tl.transaction = t.id
            JOIN item i ON tl.item = i.id
            JOIN location loc ON tl.location = loc.id
            LEFT JOIN unitstypeuom u ON u.internalid = tl.units
            LEFT JOIN (SELECT ibq.item, b.location, MIN(ibq.bin) AS PreferredBinId
                FROM itembinquantity ibq JOIN bin b ON b.id = ibq.bin
                WHERE ibq.preferredbin = 'T' GROUP BY ibq.item, b.location) pref
                ON pref.item = tl.item AND pref.location = tl.location
            LEFT JOIN (SELECT iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location AS LocationId,
                    MIN(ba.custrecord_dbti_vba_assigned_bin) AS AssignedBinId
                FROM itemvendor iv
                JOIN customrecord_dbti_vendor_bin_assignment ba ON ba.custrecord_dbti_vba_vendor = iv.vendor
                WHERE iv.preferredvendor = 'T'
                GROUP BY iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location) vendorbin
                ON vendorbin.item = tl.item AND vendorbin.subsidiary = t.subsidiary
                AND vendorbin.LocationId = tl.location
            LEFT JOIN (SELECT item, location, inventorystatus, SUM(quantityavailable) AS Available
                FROM inventorybalance GROUP BY item, location, inventorystatus) locstock
                ON locstock.item = tl.item AND locstock.location = tl.location
                AND locstock.inventorystatus = {status}
            LEFT JOIN (SELECT item, location, binnumber, inventorystatus, SUM(quantityavailable) AS Available
                FROM inventorybalance GROUP BY item, location, binnumber, inventorystatus) binstock
                ON binstock.item = tl.item AND binstock.location = tl.location
                AND binstock.binnumber = COALESCE(vendorbin.AssignedBinId, pref.PreferredBinId)
                AND binstock.inventorystatus = {status}
            WHERE t.id = {orderId} AND tl.transactionlinetype = 'SHIPPING'
            ORDER BY tl.id
            """;
        var rows = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            (limit, offset) => client.ExecuteSuiteQLQuery<LineRow>(query, limit, offset, ct),
            1000, 100, $"STR {orderId} inventory read");
        var countQuery = $"""
            SELECT COUNT(*) AS OpenCount FROM transactionline tl
            WHERE tl.transaction = {orderId} AND tl.transactionlinetype = 'SHIPPING'
              AND tl.isclosed = 'F' AND ABS(tl.quantity) > ABS(NVL(tl.quantityshiprecv, 0))
            """;
        var counted = await client.ExecuteSuiteQLQuery<OpenCountRow>(countQuery, 1, 0, ct);
        if (counted?.items is null || counted.items.Count != 1)
            throw new InvalidOperationException("The authoritative STR open-line count is unavailable.");

        var mapped = rows.Select(row =>
        {
            var binRequired = row.UseBins switch { "T" => true, "F" => false, _ => (bool?)null };
            var stock = new AutoFulfillmentStockLineDTO(row.LineId, row.ItemId, row.LocationId,
                row.InventoryStatusId, row.BinId, binRequired, row.OrderedQuantity,
                row.ShippedQuantity, row.Available, row.UomRate, row.IsClosed == "T");
            var post = new PostTransferOrderDTO
            {
                NetsuiteOrderInternalId = orderId,
                OrderNumber = order.OrderNumber,
                OrderType = order.TransferCategoryId is 2 or 3 or 4 ? "intercompanytransferorder" : "transferorder",
                TransferCategory = order.TransferCategoryId,
                NetsuiteFromLocationInternalId = row.LocationId,
                NetsuiteToLocationInternalId = order.ToLocationId,
                NetsuiteMaterialInternalId = row.ItemId,
                MaterialCode = row.MaterialCode ?? string.Empty,
                MaterialName = row.MaterialName ?? string.Empty,
                LineSequenceNumber = row.LineId,
                LineQuantity = row.OrderedQuantity,
                QuantityAlreadyShipped = row.ShippedQuantity,
                UoMRate = row.UomRate,
                IsClosed = row.IsClosed,
                IsLocationUsedBin = binRequired == true,
                LocationUsedBin = row.UseBins ?? string.Empty,
                NetsuiteMaterialPrefferedBinId = row.BinId ?? 0,
                NetsuiteMaterialVendorAssignedBin = row.BinId is not null && row.BinId != 0 ? row.BinId.Value : 0,
                VerifiedInventoryStatusId = row.InventoryStatusId?.ToString(CultureInfo.InvariantCulture),
                IsBad = row.InventoryStatusId == int.Parse(InventoryStatusMap.Bad, CultureInfo.InvariantCulture),
                IsMissing = row.InventoryStatusId == int.Parse(InventoryStatusMap.Missing, CultureInfo.InvariantCulture)
            };
            return new AutoStrLineDTO(stock, post);
        }).ToArray();
        return (mapped, counted.items[0].OpenCount);
    }

    public async Task<IReadOnlyList<int>> GetFulfillmentIdsAsync(int orderId, CancellationToken ct)
    {
        if (orderId <= 0) throw new ArgumentOutOfRangeException(nameof(orderId));
        var query = $"""
            SELECT t.id AS Id FROM transaction t
            JOIN transactionline tl ON tl.transaction = t.id AND tl.mainline = 'T'
            WHERE t.type = 'ItemShip' AND tl.createdfrom = {orderId}
            ORDER BY t.id
            """;
        var rows = await NetSuiteApiClientService.ReadAllSuiteQLPagesAsync(
            (limit, offset) => client.ExecuteSuiteQLQuery<FulfillmentRow>(query, limit, offset, ct),
            1000, 100, $"STR {orderId} fulfillment reconciliation");
        return rows.Select(x => x.Id).Distinct().ToArray();
    }

    public async Task PostShippedAsync(IReadOnlyList<AutoStrLineDTO> lines, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (lines.Count == 0 || lines.Select(x => x.Posting.NetsuiteOrderInternalId).Distinct().Count() != 1)
            throw new InvalidOperationException("Exactly one STR with open lines is required for auto-fulfillment.");
        if (!await client.SaveAutoTOItemFulfillment(lines.Select(x => x.Posting).ToList()))
            throw new InvalidOperationException("NetSuite did not confirm the auto-fulfillment save.");
    }

    private static void ValidateApprovalField(string field)
    {
        if (!StrAutoFulfillmentOptions.IsApprovalTimeField(field))
            throw new InvalidOperationException("A verified NetSuite STR approval-time body field is required.");
    }

    private static AutoStrOrderDTO Map(OrderRow row)
    {
        if (!DateTime.TryParseExact(row.ApprovedAt, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var at))
            throw new InvalidOperationException($"STR {row.NetsuiteOrderId} has no valid approved-at time.");
        return new AutoStrOrderDTO(row.NetsuiteOrderId, row.OrderNumber ?? string.Empty,
            row.TransferCategoryId, row.FromLocationId, row.ToLocationId, row.SubsidiaryId,
            row.ApprovalStatusId, DateTime.SpecifyKind(at, DateTimeKind.Utc));
    }

    private sealed class OrderRow
    {
        public int NetsuiteOrderId { get; set; }
        public string? OrderNumber { get; set; }
        public int TransferCategoryId { get; set; }
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
        public int SubsidiaryId { get; set; }
        public int ApprovalStatusId { get; set; }
        public string? ApprovedAt { get; set; }
    }

    private sealed class LineRow
    {
        public int LineId { get; set; }
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public int? InventoryStatusId { get; set; }
        public int? BinId { get; set; }
        public string? UseBins { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal ShippedQuantity { get; set; }
        public decimal? Available { get; set; }
        public decimal UomRate { get; set; }
        public string IsClosed { get; set; } = string.Empty;
        public string? MaterialCode { get; set; }
        public string? MaterialName { get; set; }
    }

    private sealed class OpenCountRow { public int OpenCount { get; set; } }
    private sealed class FulfillmentRow { public int Id { get; set; } }
}
