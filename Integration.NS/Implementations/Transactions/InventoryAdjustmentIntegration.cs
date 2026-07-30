using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using Integration.NS.DataTransferObjects.InventoryAdjustment;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Mapster;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System.Text.Json;

namespace Integration.NS.Implementations.Transactions;

public class InventoryAdjustmentIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory) : IInventoryAdjustmentIntegration
{
    public async Task<InventoryAdjustmentDTO?> GetInventoryAdjustmentAsync(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(InventoryAdjustmentNSDTO.Id)),
                ("t.tranid", nameof(InventoryAdjustmentNSDTO.ReferenceNumber)),
                ("t.memo", nameof(InventoryAdjustmentNSDTO.Memo)),
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(InventoryAdjustmentNSDTO.PreparedBy)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentNSDTO.LocationName)),
                ("tl.location", nameof(InventoryAdjustmentNSDTO.LocationId)),
                ("BUILTIN.DF(t.account)", nameof(InventoryAdjustmentNSDTO.AccountName)),
                ("t.account", nameof(InventoryAdjustmentNSDTO.AccountId)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryAdjustmentNSDTO.SubsidiaryName)),
                ("t.subsidiary", nameof(InventoryAdjustmentNSDTO.SubsidiaryId)),
                ("NVL(t.custbody_atlas_inv_adj_reason, -1)", nameof(InventoryAdjustmentNSDTO.ReasonId)),
                ("iar.name", nameof(InventoryAdjustmentNSDTO.ReasonName)),
                ("iar.custrecord_atlas_glaccount", nameof(InventoryAdjustmentNSDTO.ReasonAccountId)),
                ("BUILTIN.DF(iar.custrecord_atlas_glaccount)", nameof(InventoryAdjustmentNSDTO.ReasonAccountName)),
                ("iac.id", nameof(InventoryAdjustmentNSDTO.CategoryId)),
                ("iac.name", nameof(InventoryAdjustmentNSDTO.CategoryName)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS') ", nameof(InventoryAdjustmentNSDTO.Date))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("CUSTOMRECORD_ATLAS_INV_ADJ_REASN iar", on: "iar.id = t.custbody_atlas_inv_adj_reason")
            .LeftJoin("CUSTOMLIST_DBTI_ADJUSTMENT_CATEGORY_LI iac", on: "iac.id = t.custbody_dbti_adjustment_category")
            .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "inventoryadjustment"),
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "T")
            )
            .WithSubsidiaries(httpContextAccessor, "t")
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryAdjustmentNSDTO>(query.Query, query.Limit, query.Offset);

        var nsdto = response.items.FirstOrDefault();
        if (nsdto is null) return null;

        var result = nsdto.Adapt<InventoryAdjustmentDTO>();

        result.Subsidiary = new SubsidiaryDTO { Id = nsdto.SubsidiaryId, Name = nsdto.SubsidiaryName };
        result.Location = new LocationDTO { Id = nsdto.LocationId, Name = nsdto.LocationName };
        result.Account = new BusinessAccountDTO { Id = nsdto.AccountId, Name = nsdto.AccountName };
        result.Category = new InventoryAdjustmentCategoryDTO { Id = nsdto.CategoryId, Name = nsdto.CategoryName };
        result.Reason = nsdto.ReasonId < 0 ? null : new InventoryAdjustmentReasonDTO 
        { 
            Name = nsdto.ReasonName,
            Id  = nsdto.ReasonId,
            AccountId = nsdto.AccountId,
            AccountName = nsdto.ReasonAccountName
        };
        return result;
    }

    public async Task<IEnumerable<InventoryAdjustmentLineDTO>> GetInventoryAdjustmentLinesAsync(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemId", nameof(InventoryAdjustmentLineNSDTO.ItemCode)),
                ("item.displayname", nameof(InventoryAdjustmentLineNSDTO.ItemDescription)),
                ("item.usebins", nameof(InventoryAdjustmentLineNSDTO.ItemUseBins)),
                ("BUILTIN.DF(tl.units)", nameof(InventoryAdjustmentLineNSDTO.UoMName)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentLineNSDTO.LocationName)),
                ("tl.units", nameof(InventoryAdjustmentLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(InventoryAdjustmentLineNSDTO.UoMRate)),
                ("tl.location", nameof(InventoryAdjustmentLineNSDTO.LocationId)),
                ("tl.quantity", nameof(InventoryAdjustmentLineNSDTO.QuantityAlloted)),
                ("tl.id", nameof(InventoryAdjustmentLineNSDTO.LineNumber)),
                ("iil.quantityonhand", nameof(InventoryAdjustmentLineNSDTO.QuantityOnHand))
            )
            .From("transactionline tl")
            .Join("item item", on: "item.id = tl.item")
            .Join("transaction t", on: "t.id = tl.transaction")
            .Join("transactionline ml", on: "ml.transaction = t.id AND ml.mainline = 'T'")
            .LeftJoin("unitstypeuom uom", on: "tl.units = uom.internalid")
            .LeftJoin("inventoryitemlocations iil", on: "tl.item = iil.item AND ml.location = iil.location")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "F")
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryAdjustmentLineNSDTO>(query.Query);
        return response.items.Select(ConvertLine);
    }

    public async Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetInventoryAdjustmentsAsync(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(InventoryAdjustmentDataGridDTO.ReferenceNumber)));

        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(InventoryAdjustmentDataGridDTO.Id)),
                ("t.tranid", nameof(InventoryAdjustmentDataGridDTO.ReferenceNumber)),
                ("t.memo", nameof(InventoryAdjustmentDataGridDTO.Memo)),
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(InventoryAdjustmentDataGridDTO.PreparedBy)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentDataGridDTO.Location)),
                ("BUILTIN.DF(t.account)", nameof(InventoryAdjustmentDataGridDTO.Account)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryAdjustmentDataGridDTO.Subsidiary)),
                ("iar.name", nameof(InventoryAdjustmentDataGridDTO.AdjustmentReason)),
                ("NVL(receipt.total, 0)", nameof(InventoryAdjustmentDataGridDTO.QuantityReceivedTotal)),
                ("NVL(issue.total, 0)", nameof(InventoryAdjustmentDataGridDTO.QuantityIssuedTotal)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS') ", nameof(InventoryAdjustmentDataGridDTO.Date)),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS') ", nameof(InventoryAdjustmentDataGridDTO.DateLastModified))
            )
            .From("transaction t")
            .Join("transactionline tl", on:"tl.transaction = t.id")
            .LeftJoin("CUSTOMRECORD_ATLAS_INV_ADJ_REASN iar", on: "iar.id = t.custbody_atlas_inv_adj_reason")
            .LeftJoin($"({ReceiptQuery}) receipt", on: "receipt.transactionid = t.id")
            .LeftJoin($"({IssueQuery}) issue", on: "issue.transactionid = t.id")
            .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "inventoryadjustment"),
                DataGridFilterUtilities.Equal("tl.mainline", "T")
            )
            .WithDatagridIntent(intent)
            .WithSubsidiaries(httpContextAccessor, "t")
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryAdjustmentDataGridDTO>(query.Query, query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }
    public Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetReceiptsAdjustmentsAsync(DataGridIntent intent)
    {
        DataGridIntent newIntent = intent.Adapt<DataGridIntent>();
        newIntent.Filters.AddRange([
            DataGridFilterUtilities.GreaterThan(nameof(InventoryAdjustmentDataGridDTO.QuantityReceivedTotal), 0),
            DataGridFilterUtilities.LessThanOrEqual(nameof(InventoryAdjustmentDataGridDTO.QuantityIssuedTotal), 0),
        ]);
        return GetInventoryAdjustmentsAsync(newIntent);
    }
    public Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetIssuesAdjustmentsAsync(DataGridIntent intent)
    {
        DataGridIntent newIntent = intent.Adapt<DataGridIntent>();
        newIntent.Filters.AddRange([
            DataGridFilterUtilities.GreaterThan(nameof(InventoryAdjustmentDataGridDTO.QuantityIssuedTotal), 0),
            DataGridFilterUtilities.LessThanOrEqual(nameof(InventoryAdjustmentDataGridDTO.QuantityReceivedTotal), 0),
        ]);
        return GetInventoryAdjustmentsAsync(newIntent);
    }

    public async Task<(IEnumerable<InventoryAdjustmentReasonDTO> Data, int Count)> GetInventoryAdjustmentReasonsAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("custrecord_atlas_glaccount", nameof(InventoryAdjustmentReasonDTO.AccountId)),
                ("BUILTIN.DF(custrecord_atlas_glaccount)", nameof(InventoryAdjustmentReasonDTO.AccountName)),
                ("name", nameof(InventoryAdjustmentReasonDTO.Name)),
                ("id", nameof(InventoryAdjustmentReasonDTO.Id))
            )
            .From("CUSTOMRECORD_ATLAS_INV_ADJ_REASN")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<InventoryAdjustmentReasonDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<InventoryAdjustmentCategoryDTO> Data, int Count)> GetInventoryAdjustmentCategoriesAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(InventoryAdjustmentCategoryDTO.Id)),
                ("name", nameof(InventoryAdjustmentCategoryDTO.Name))
            )
            .From("CUSTOMLIST_DBTI_ADJUSTMENT_CATEGORY_LI")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<InventoryAdjustmentCategoryDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }


    public async Task<bool> CreateInventoryAdjustment(InventoryAdjustmentDTO value)
    {
        string payloadString = CreateIAPayload(value);
        var url = $"{netsuiteService.GetRestletURI}?script=1938&deploy=1";
        try
        {
            _ = await netsuiteService.MakeRequestOAuth1<object>(url, payloadString);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }

        return true;
    }

    private string CreateIAPayload(InventoryAdjustmentDTO dto)
    {
        var issueLinesCount = dto.Lines.Count(x => x.QuantityAlloted < 0);
        var receiptLinesCount = dto.Lines.Count(x => x.QuantityAlloted > 0);
        if (issueLinesCount > 0 && receiptLinesCount > 0)
            throw new Exception("WMS does not allow goods issue lines and goods receipt lines in the same document");
        int category = issueLinesCount > 0
            ? 1 : // issue category
            2; // receipt category

        var anon = new
        {
            subsidiary = dto.Subsidiary?.Id,
            location = dto.Location?.Id,
            account = dto.Account?.Id,
            locationUsesBins = dto.Lines.Any(x => x.InventoryDetails.Any(y => y.Bin is not null)), // why is this a required field
            memo = dto.Memo,
            adjustmentReason = dto.Reason?.Id,
            preparedBy = dto.PreparedById,
            trandate = dto.Date.ToString("MM/dd/yyyy"),
            adjustmentCategory = category,
            department = 15, // operations
            // this should be set here i think. integration should be responsible for logic that
            // concerns netsuite operations and setting the department/class by default is exactly that.
            // TODO would be great if there was some centralized place to store these values and also maybe strings that prompt or alert users.
            classId = 1, // external
            lines = dto.Lines.Select(line => new
            {
                item = line.ItemId,
                quantity = line.QuantityAlloted * (line.UoM?.ConversionRate ?? 1),
                InventoryDetail = line.InventoryDetails.Select(detail => new
                {
                    status = detail.Status?.Id,
                    qty = (line.QuantityAlloted < 0 ? -detail.QuantityAlloted : detail.QuantityAlloted) * (line.UoM?.ConversionRate ?? 1),
                    bin = detail.Bin?.Id
                })
            })
        };

        return JsonSerializer.Serialize(anon, jsonSerializerOptions);
    }

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private InventoryAdjustmentLineDTO ConvertLine(InventoryAdjustmentLineNSDTO nsdto)
    {
        var dto = nsdto.Adapt<InventoryAdjustmentLineDTO>();

        dto.Location = new LocationDTO
        {
            Name = nsdto.LocationName,
            Id = nsdto.LocationId
        };
        dto.UoM = new ItemUnitDTO
        {
            Name = nsdto.UoMName,
            Id = nsdto.UoMId,
            ConversionRate = nsdto.UoMRate
        };
        return dto;
    }


    readonly string IssueQuery = """
            SELECT
                count(itl.id) AS total,
                itl.transaction AS transactionid
            FROM
                transactionline itl
                JOIN transaction it ON it.recordtype = 'inventoryadjustment'
                AND it.id = itl.transaction
            WHERE
                itl.quantity <= 0
                AND itl.mainline = 'F'
            GROUP BY itl.transaction 
        """;

    readonly string ReceiptQuery = """
            SELECT
                count(rtl.id) AS total,
                rtl.transaction AS transactionid
            FROM
                transactionline rtl
                JOIN transaction rt ON rt.recordtype = 'inventoryadjustment'
                AND rt.id = rtl.transaction
            WHERE
                rtl.quantity >= 0
                AND rtl.mainline = 'F'
            GROUP BY rtl.transaction 
        """;
}
