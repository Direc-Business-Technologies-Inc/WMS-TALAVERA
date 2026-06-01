using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using B1SLayer;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.GoodsIssue;
using Integration.SAP.Entities.Transactional.GoodsReceipt;
using Shared.Entities;

namespace Integration.SAP.Implementations.Transaction.GoodsReceipt;

public class GoodsReceiptIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IGoodsReceiptIntegration
{
    public async Task<(IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count)> GetApprovedGoodsReceiptDataGrid(DataGridIntent intent)
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

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_GoodsReceipt_DataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Approved Goods Receipt not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<GoodsReceiptDataGridSAPDTO> docs = await SLActions.RawQueryAsync<GoodsReceiptDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<(IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count)> GetGoodsReceiptDraftDataGrid(DataGridIntent intent, string status)
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

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_GoodsReceipt_DraftDataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Approved Goods Receipt not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<GoodsReceiptDataGridSAPDTO> docs = await SLActions.RawQueryAsync<GoodsReceiptDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<GoodsReceiptHeaderSAPDTO?> GetGoodsReceiptDraftHeader(int docEntry)
    {
        GoodsReceiptHeaderSAPDTO? doc = await SLActions.SingleAsync<GoodsReceiptHeaderSAPDTO, object>("APHI_GoodsReceipt_DraftsHeader", new { ObjType = 59, DocEntry = docEntry });

        return doc;
    }

    public async Task<IEnumerable<GoodsReceiptLineSAPDTO>> GetGoodsReceiptDraftLines(int docEntry)
    {
        IEnumerable<GoodsReceiptLineSAPDTO> doc = await SLActions.QueryAsync<GoodsReceiptLineSAPDTO, object>("APHI_GoodsReceipt_DraftsLines", new { ObjType = 59, DocEntry = docEntry });

        return doc;
    }

    public async Task<GoodsReceiptHeaderSAPDTO?> GetGoodsReceiptHeader(int docEntry)
    {
        GoodsReceiptHeaderSAPDTO? doc = await SLActions.SingleAsync<GoodsReceiptHeaderSAPDTO, object>("APHI_GoodsReceipt_Header", new { docEntry });

        return doc;
    }

    public async Task<IEnumerable<GoodsReceiptLineSAPDTO>> GetGoodsReceiptLines(int docEntry)
    {
        IEnumerable<GoodsReceiptLineSAPDTO> doc = await SLActions.QueryAsync<GoodsReceiptLineSAPDTO, object>("APHI_GoodsReceipt_Lines", new { docEntry });

        return doc;
    }

    public async Task<bool> PostGoodsReceipt(GoodsReceiptDTO data)
    {
        List<InventoryGenEntryLinesPayload> payloadLines = [];

        foreach (GoodsReceiptLineDTO? line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new(line.ItemCode, line.Warehouse.WhsCode, data.TransactionType.Account.AcctCode, line.Quantity));

        InventoryGenEntryPayload payload = new(data.PreparedBy,
                                               data.TransactionType.Code,
                                               payloadLines,
                                               data.BusinessPartner.CardCode,
                                               data.BusinessPartner.CardName,
                                               data.PurNo,
                                               data.WarNo,
                                               data.Designation,
                                               data.ReceivedBy,
                                               data.DocRemarks,
                                               data.ApprovedBy,
                                               data.NotedBy);

        try
        {
            await SLActions.PostAsync<object, InventoryGenEntryPayload>("InventoryGenEntries", payload);
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
