using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using B1SLayer;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.GoodsIssue;
using Mapster.Adapters;
using Shared.Entities;
using System.Text.Json;

namespace Integration.SAP.Implementations.Transaction.GoodsIssue;

public class GoodsIssueIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IGoodsIssueIntegration
{
    public async Task<(IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count)> GetApprovedGoodsIssueDataGrid(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "PreparedBy", "T0.U_PrepBy" },
                { "TransTypeCode", "T1.U_TransType" },
                { "TransTypeName", "T1.Name" },
                { "AcctCode", "T2.AcctCode" },
                { "AcctName", "T2.AcctName" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_GoodsIssue_DataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Approved Goods Issue not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<GoodsIssueDataGridSAPDTO> docs = await SLActions.RawQueryAsync<GoodsIssueDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<(IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count)> GetGoodsIssueDraftDataGrid(DataGridIntent intent, string status)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "PreparedBy", "T0.U_PrepBy" },
                { "TransTypeCode", "T1.U_TransType" },
                { "TransTypeName", "T1.Name" },
                { "AcctCode", "T2.AcctCode" },
                { "AcctName", "T2.AcctName" },
                { "Status", "T1.Status" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        intent.Filters.Add(new AppFilterDescriptor
        {
            LogicalOperator = LogicalOperatorEnum.AND,
            Property = "Status",
            ComparisonOperator = ComparisonOperatorEnum.Equals,
            Value = status,
            FilterValueType = FilterValueTypeEnum.String
        });

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_GoodsIssue_DraftDataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Approved Goods Issue not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<GoodsIssueDataGridSAPDTO> docs = await SLActions.RawQueryAsync<GoodsIssueDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<GoodsIssueHeaderSAPDTO?> GetGoodsIssueDraftHeader(int docEntry)
    {
        GoodsIssueHeaderSAPDTO? doc = await SLActions.SingleAsync<GoodsIssueHeaderSAPDTO, object>("APHI_GoodsIssue_DraftsHeader", new { ObjType = 60, DocEntry = docEntry });

        return doc;
    }

    public async Task<IEnumerable<GoodsIssueLineSAPDTO>> GetGoodsIssueDraftLines(int docEntry)
    {
        IEnumerable<GoodsIssueLineSAPDTO> doc = await SLActions.QueryAsync<GoodsIssueLineSAPDTO, object>("APHI_GoodsIssue_DraftsLines", new { ObjType = 60, DocEntry = docEntry });

        return doc;
    }

    public async Task<GoodsIssueHeaderSAPDTO?> GetGoodsIssueHeader(int docEntry)
    {
        GoodsIssueHeaderSAPDTO? doc = await SLActions.SingleAsync<GoodsIssueHeaderSAPDTO, object>("APHI_GoodsIssue_Header", new { docEntry });

        return doc;
    }

    public async Task<IEnumerable<GoodsIssueLineSAPDTO>> GetGoodsIssueLines(int docEntry)
    {
        IEnumerable<GoodsIssueLineSAPDTO> doc = await SLActions.QueryAsync<GoodsIssueLineSAPDTO, object>("APHI_GoodsIssue_Lines", new { docEntry });

        return doc;
    }

    public async Task<bool> PostGoodsIssue(GoodsIssueDTO data)
    {
        List<InventoryGenExitLinesPayload> payloadLines = [];

        foreach (GoodsIssueLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new(line.ItemCode, line.Warehouse.WhsCode, data.TransactionType.Account.AcctCode, line.Quantity));

        InventoryGenExitPayload payload = new(data.PreparedBy,
                                              data.TransactionType.Code,
                                              payloadLines,
                                              data.BusinessPartner?.CardCode,
                                              data.BusinessPartner?.CardName,
                                              data.SchoolYear,
                                              data.SrfNo,
                                              data.Designation,
                                              data.DocRemarks,
                                              data.ApprovedBy,
                                              data.ReceivedBy,
                                              data.NotedBy);

        string jsonString = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        try
        {
            await SLActions.PostAsync<object, InventoryGenExitPayload>("InventoryGenExits", payload);
        }
        catch (SLException ex) when (ex.Message.Contains("-2028"))
        {

        }
        catch
        {
            throw;
        }

        return true;
    }
}
