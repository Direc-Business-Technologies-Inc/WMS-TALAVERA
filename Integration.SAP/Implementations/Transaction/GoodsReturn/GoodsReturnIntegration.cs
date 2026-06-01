using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.GoodsReturn;
using Shared.Entities;
using System.Text.Json;

namespace Integration.SAP.Implementations.Transaction.GoodsReturn;

public class GoodsReturnIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IGoodsReturnIntegration
{
    public async Task<GoodsReturnHeaderSAPDTO?> GetGoodsReturnHeaderAsync(int docEntry)
    {
        GoodsReturnHeaderSAPDTO? doc = await SLActions.SingleAsync<GoodsReturnHeaderSAPDTO, object>("APHI_GoodsReturn_GoodsReturnHeader", new { docEntry });

        return doc;
    }

    public async Task<IEnumerable<GoodsReturnLineSAPDTO>> GetGoodsReturnLinesAsync(int docEntry)
    {
        List<GoodsReturnLineSAPDTO>? lines = await SLActions.QueryAsync<GoodsReturnLineSAPDTO, object>("APHI_GoodsReturn_GoodsReturnLines", new { docEntry });

        return lines;
    }

    public async Task<(IEnumerable<GoodsReturnsSAPDTO>, int)> GetGoodsReturnsListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "LineNum", "T1.LineNum" },
                { "GRPODocEntry", "T3.BaseEntry" },
                { "GRPODocNum", "T3.DocNum" },
                { "PODocEntry", "T5.DocEntry" },
                { "PODocNum", "T5.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "DocDueDate", "T0.DocDueDate" },
                { "CardCode", "T0.CardCode" },
                { "CardName", "T0.CardName" },
                { "Remarks", "T0.Comments" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_GoodsReturn_GoodsReturns", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all open Purchase Orders not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<GoodsReturnsSAPDTO> docs = await SLActions.RawQueryAsync<GoodsReturnsSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<GRRHeaderSAPDTO?> GetGRRHeaderAsync(int docEntry)
    {
        GRRHeaderSAPDTO? doc = await SLActions.SingleAsync<GRRHeaderSAPDTO, object>("APHI_GoodsReturn_GRRHeader", new { docEntry });

        return doc;
    }

    public async Task<IEnumerable<GRRLineSAPDTO>> GetGRRLinesAsync(int docEntry)
    {
        List<GRRLineSAPDTO>? lines = await SLActions.QueryAsync<GRRLineSAPDTO, object>("APHI_GoodsReturn_GRRLines", new { docEntry });

        return lines;
    }

    public async Task<(IEnumerable<GoodsReturnRequestsSAPDTO>, int)> GetGRRsListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "LineNum", "T1.LineNum" },
                { "GRPODocEntry", "T3.BaseEntry" },
                { "GRPODocNum", "T3.DocNum" },
                { "PODocEntry", "T5.DocEntry" },
                { "PODocNum", "T5.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "DocDueDate", "T0.DocDueDate" },
                { "CardCode", "T0.CardCode" },
                { "CardName", "T0.CardName" },
                { "Remarks", "T0.Comments" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_GoodsReturn_GoodsReturnRequests", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all open Purchase Orders not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<GoodsReturnRequestsSAPDTO> docs = await SLActions.RawQueryAsync<GoodsReturnRequestsSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<bool> PostGoodsReturnFromGRRAsync(GoodsReturnDTO data)
    {
        List<object> payloadLines = [];

        foreach (GoodsReturnLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new PurchaseReturnLinesPayload(data.SapReference.BaseEntry, 234000032, line.LineNum, data.DocumentLines.IndexOf(line), line.ItemCode, line.UoMCode, line.Quantity, line.Warehouse.WhsCode));

        PurchaseReturnPayload payload = new(data.DocDate,
                                            data.DocDueDate,
                                            data.BusinessPartner.CardCode,
                                            data.ReturnType,
                                            data.PreparedBy,
                                            payloadLines,
                                            data.SchoolYear,
                                            data.DRNo,
                                            data.SINo,
                                            data.DeliveredBy,
                                            data.ReceivedBy,
                                            data.DocRemarks,
                                            data.ApprovedBy,
                                            data.CheckedBy);

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        await SLActions.PostAsync<object, PurchaseReturnPayload>("PurchaseReturns", payload);

        return true;
    }

    public async Task<bool> PostGoodsReturnFromGRPOAsync(GoodsReturnDTO data)
    {
        List<object> payloadLines = [];

        foreach (GoodsReturnLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new PurchaseReturnLinesPayload(data.GRPODocEntry, 20, line.LineNum, data.DocumentLines.IndexOf(line), line.ItemCode, line.UoMCode, line.Quantity, line.Warehouse.WhsCode));

        PurchaseReturnPayload payload = new(data.DocDate,
                                            data.DocDueDate,
                                            data.BusinessPartner.CardCode,
                                            data.ReturnType,
                                            data.PreparedBy,
                                            payloadLines,
                                            data.SchoolYear,
                                            data.DRNo,
                                            data.SINo,
                                            data.DeliveredBy,
                                            data.ReceivedBy,
                                            data.DocRemarks,
                                            data.ApprovedBy,
                                            data.ReviewedBy,
                                            data.CheckedBy);

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        await SLActions.PostAsync<object, PurchaseReturnPayload>("PurchaseReturns", payload);

        return true;
    }

    public async Task<bool> PostGoodsReturnAsync(GoodsReturnDTO data)
    {
        List<object> payloadLines = [];

        foreach (GoodsReturnLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new StandalonePurchaseReturnLinesPayload(data.DocumentLines.IndexOf(line), line.ItemCode, line.UoMCode, line.Quantity, line.Warehouse.WhsCode));

        PurchaseReturnPayload payload = new(data.DocDate,
                                            data.DocDueDate,
                                            data.BusinessPartner.CardCode,
                                            data.ReturnType,
                                            data.PreparedBy,
                                            payloadLines,
                                            data.SchoolYear,
                                            data.DRNo,
                                            data.SINo,
                                            data.DeliveredBy,
                                            data.ReceivedBy,
                                            data.DocRemarks,
                                            data.ApprovedBy,
                                            data.ReviewedBy,
                                            data.CheckedBy);

        await SLActions.PostAsync<object, PurchaseReturnPayload>("PurchaseReturns", payload);

        return true;
    }

    public async Task<IEnumerable<ReturnTypeSAPDTO>> GetReturnTypesAsync()
    {
        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_GoodsReturn_ReturnTypes", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Return Types not found.");

        List<ReturnTypeSAPDTO> data = await SLActions.RawQueryAsync<ReturnTypeSAPDTO>(qry);

        return data;
    }
}
