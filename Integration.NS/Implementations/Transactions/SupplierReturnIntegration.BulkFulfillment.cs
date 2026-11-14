using System.Globalization;
using System.Net;
using System.Text.Json;
using Shared.Entities;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.DataTransferObjects.Others.NS;
using Domain.Providers;
using Integration.NS.DataTransferObjects.SupplierReturn;
using Integration.NS.Exceptions;
using Integration.NS.Helpers;
using Shared.Libraries.Utilities;

namespace Integration.NS.Implementations.Transactions;

public partial class SupplierReturnIntegration
{
    public async Task<(IEnumerable<BulkFulfillmentStatusDTO> Data, int Count)> GetBulkFulfillmentStatusesAsync(
        DataGridIntent intent, IReadOnlyList<int> allowedSubsidiaries, CancellationToken ct = default)
    {
        if (allowedSubsidiaries.Count == 0) return ([], 0);
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(BulkFulfillmentStatusDTO.Id)));
        var query = builderFactory.Create()
            .Select(("bf.id", nameof(BulkFulfillmentStatusDTO.Id)),
                ("TO_CHAR(bf.custrecord_bf_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(BulkFulfillmentStatusDTO.Date)),
                ("bf.custrecord_bf_memo", nameof(BulkFulfillmentStatusDTO.Memo)),
                ("BUILTIN.DF(bf.custrecord_bf_status)", nameof(BulkFulfillmentStatusDTO.Status)),
                ("bf.custrecord_bf_error_message", nameof(BulkFulfillmentStatusDTO.ErrorMessage)))
            .From("customrecord_bulk_fulfillment bf")
            .WithDatagridIntent(intent, new Dictionary<string, string>
            {
                [nameof(BulkFulfillmentStatusDTO.Date)] = "bf.custrecord_bf_date",
                [nameof(BulkFulfillmentStatusDTO.Memo)] = "bf.custrecord_bf_memo",
                [nameof(BulkFulfillmentStatusDTO.Status)] = "BUILTIN.DF(bf.custrecord_bf_status)",
                [nameof(BulkFulfillmentStatusDTO.Id)] = "bf.id"
            }).Build();
        var response = await query.ExecuteWithPaging<BulkFulfillmentStatusDTO>(netsuiteService, ct);
        return (response.items, response.totalResults);
    }

    public async Task<IReadOnlyList<SupplierReturnDataGridDTO>> GetBulkFulfillmentReturnsAsync(
        int bulkId, IReadOnlyList<int> allowedSubsidiaries, CancellationToken ct = default)
    {
        if (bulkId <= 0 || allowedSubsidiaries.Count == 0) return [];
        var referencesQuery = builderFactory.Create()
            .Select(("bf.custrecord_bf_return_to_supplier_ids", nameof(BulkFulfillmentReturnIds.ReturnIds)))
            .From("customrecord_bulk_fulfillment bf")
            .WithFilters(DataGridFilterUtilities.Equal("bf.id", bulkId))
            .Build();
        var references = await netsuiteService.ExecuteSuiteQLQuery<BulkFulfillmentReturnIds>(
            referencesQuery.Query, limit: 1, ct: ct);
        var rawIds = references.items.FirstOrDefault()?.ReturnIds;
        if (string.IsNullOrWhiteSpace(rawIds)) return [];
        var ids = new List<int>();
        foreach (var token in rawIds.Split(','))
        {
            if (!int.TryParse(token.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                throw new InvalidOperationException($"NetSuite returned an invalid RTS reference for bulk fulfillment {bulkId}.");
            if (!ids.Contains(id)) ids.Add(id);
        }
        if (ids.Count == 0) return [];
        var query = builderFactory.Create()
            .Select(("t.id", nameof(SupplierReturnDataGridDTO.Id)),
                ("t.tranid", nameof(SupplierReturnDataGridDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(SupplierReturnDataGridDTO.Date)),
                ("BUILTIN.DF(t.entity)", nameof(SupplierReturnDataGridDTO.VendorName)),
                ("BUILTIN.DF(t.custbody_dbti_return_category)", nameof(SupplierReturnDataGridDTO.CategoryName)),
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(SupplierReturnDataGridDTO.PreparedBy)))
            .From("transaction t")
            .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
            .WithFilters(DataGridFilterUtilities.Equal("t.recordtype", "vendorreturnauthorization"),
                DataGridFilterUtilities.In("t.id", ids.ToArray()),
                DataGridFilterUtilities.In("t.subsidiary", allowedSubsidiaries.ToArray()))
            .Build();
        var response = await netsuiteService.ExecuteSuiteQLQuery<SupplierReturnDataGridDTO>(query.Query, limit: ids.Count, ct: ct);
        return response.items.ToArray();
    }

    public async Task<BulkFulfillmentResultDTO> SubmitBulkFulfillmentAsync(SubmitBulkFulfillmentDTO data,
        IReadOnlyList<int> allowedSubsidiaries, CancellationToken ct = default)
    {
        var ids = data.ReturnIds?.Distinct().ToArray() ?? [];
        if (ids.Length < 2 || ids.Any(id => id <= 0))
            return BulkFulfillmentResultDTO.Reject("Select at least two unique RTS records with valid internal IDs.");
        if (allowedSubsidiaries.Count == 0 || allowedSubsidiaries.Any(id => id <= 0))
            return BulkFulfillmentResultDTO.Reject("Your subsidiary access could not be established. Sign in again or contact an administrator.");

        // Revalidate the entire selection against NetSuite, bypassing the grid cache.
        var query = builderFactory.Create().Select(("t.id", nameof(SupplierReturnNSDTO.Id)))
            .From("transaction t")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "vendorreturnauthorization"),
                DataGridFilterUtilities.In("t.id", ids),
                DataGridFilterUtilities.In("t.subsidiary", allowedSubsidiaries.ToArray()))
            .Build();
        NetSuiteResponse<SupplierReturnNSDTO> accessible;
        try
        {
            accessible = await netsuiteService.ExecuteSuiteQLQuery<SupplierReturnNSDTO>(query.Query, limit: ids.Length, ct: ct);
        }
        catch (Exception ex) when (ex is NetSuiteValidationException or NetSuiteTransientException or NetSuiteGateUnavailableException)
        {
            return BulkFulfillmentResultDTO.Reject("RTS access could not be verified with NetSuite. No bulk request was sent. Refresh the grid and try again, or contact an administrator.");
        }
        if (!ids.ToHashSet().SetEquals(accessible.items.Select(row => row.Id)))
            return BulkFulfillmentResultDTO.Reject("One or more selected records are no longer accessible RTS records. Refresh the grid and review your selection.");

        var payload = new BulkFulfillmentPayloadDTO(
            DateOnly.FromDateTime(DateTimeProvider.Today), ids, data.Memo);
        ct.ThrowIfCancellationRequested();
        try
        {
            var receipt = await netsuiteService.CreateRecordAsync(
                netsuiteService.GetRestAPIURI + "/record/v1/customrecord_bulk_fulfillment",
                JsonSerializer.Serialize(payload), ct);
            var id = ParseBulkFulfillmentReference(receipt.Location);
            return new(BulkFulfillmentOutcome.Submitted, id.HasValue
                ? $"Bulk fulfillment request submitted. NetSuite reference: {id}."
                : "Bulk fulfillment request submitted, but its reference could not be retrieved. Check NetSuite before submitting again.", id);
        }
        catch (NetSuiteValidationException)
        {
            return BulkFulfillmentResultDTO.Reject("NetSuite rejected the bulk fulfillment request. Ask an administrator to review the NetSuite response in the server logs for the exact reason before trying again.");
        }
        catch (NetSuiteGateUnavailableException)
        {
            return BulkFulfillmentResultDTO.Reject("NetSuite is busy; the request was not sent. Please try again shortly.");
        }
        catch (NetSuiteTransientException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return BulkFulfillmentResultDTO.Reject("NetSuite is busy and rejected the request. Please try again shortly.");
        }
        catch (Exception ex) when (ex is NetSuiteTransientException or HttpRequestException or OperationCanceledException or IOException)
        {
            return BulkFulfillmentResultDTO.Uncertain();
        }
    }

    public static int? ParseBulkFulfillmentReference(string? location)
    {
        if (string.IsNullOrWhiteSpace(location) || !Uri.TryCreate(location, UriKind.RelativeOrAbsolute, out var uri)) return null;
        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : location.Split('?', '#')[0];
        var parts = path.TrimEnd('/').Split('/');
        return parts.Length >= 2 && parts[^2].Equals("customrecord_bulk_fulfillment", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(parts[^1], NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;
    }
}
