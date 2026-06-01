using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.DataTransferObjects.Transactions.SalesReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.SalesReturn;
using Shared.Entities;

namespace Integration.SAP.Implementations.Transaction.SalesReturn;

public class SalesReturnIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : ISalesReturnIntegration
{
    public async Task<(IEnumerable<SalesReturnDataGridSAPDTO> Data, int Count)> GetSalesReturnDataAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "ORDN.DocEntry" },
                { "DocNum", "ORDN.DocNum" },
                { "DocDate", "ORDN.DocDate" },
                { "Remarks", "ORDN.U_Remarks" },
                { "CardCode", "ORDN.CardCode" },
                { "CardName", "OCRD.CardName" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocEntry",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_SalesReturn_DataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Sales Return not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<SalesReturnDataGridSAPDTO> docs = await SLActions.RawQueryAsync<SalesReturnDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<SalesReturnHeaderSAPDTO?> GetSalesReturnHeaderAsync(int docEntry)
    {
        SalesReturnHeaderSAPDTO? doc = await SLActions.SingleAsync<SalesReturnHeaderSAPDTO, object>("APHI_SalesReturn_Header", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<IEnumerable<SalesReturnLinesSAPDTO>> GetSalesReturnLinesAsync(int docEntry)
    {
        IEnumerable<SalesReturnLinesSAPDTO> doc = await SLActions.QueryAsync<SalesReturnLinesSAPDTO, object>("APHI_SalesReturn_Lines", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<(IEnumerable<SalesReturnRequestDataGridSAPDTO> Data, int Count)> GetSalesReturnRequestDataAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "ORRR.DocEntry" },
                { "DocNum", "ORRR.DocNum" },
                { "DocDate", "ORRR.DocDate" },
                { "Remarks", "ORRR.U_Remarks" },
                { "CardCode", "ORRR.CardCode" },
                { "CardName", "OCRD.CardName" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocEntry",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_SalesReturnRequest_DataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Sales Return not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<SalesReturnRequestDataGridSAPDTO> docs = await SLActions.RawQueryAsync<SalesReturnRequestDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<SalesReturnRequestHeaderSAPDTO?> GetSalesReturnRequestHeaderAsync(int docEntry)
    {
        SalesReturnRequestHeaderSAPDTO? doc = await SLActions.SingleAsync<SalesReturnRequestHeaderSAPDTO, object>("APHI_SalesReturnRequest_Header", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<IEnumerable<SalesReturnRequestLinesSAPDTO>> GetSalesReturnRequestLinesAsync(int docEntry)
    {
        IEnumerable<SalesReturnRequestLinesSAPDTO> doc = await SLActions.QueryAsync<SalesReturnRequestLinesSAPDTO, object>("APHI_SalesReturnRequest_Lines", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<IEnumerable<ReturnTypeSAPDTO>> GetReturnTypesAsync()
    {
        IEnumerable<ReturnTypeSAPDTO> returnTypes = await SLActions.QueryAsync<ReturnTypeSAPDTO, object>("APHI_SalesReturn_ReturnTypes", new { });

        return returnTypes;
    }

    public async Task<bool> PostSalesReturnAsync(SalesReturnDTO data)
    {
        List<object> payloadLines = [];

        foreach (SalesReturnLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new StandaloneSalesReturnLinesPayload(
                data.DocumentLines.IndexOf(line),
                line.ItemCode,
                line.UoMCode,
                line.Quantity,
                line.Warehouse?.WhsCode ?? string.Empty));

        SalesReturnPayload payload = new(
            data.DocDate,
            data.DocDueDate,
            data.BusinessPartner.CardCode,
            data.PreparedBy,
            payloadLines,
            data.ReturnType,
            data.SchoolYear,
            data.DRNo,
            data.SINo,
            data.PURNo,
            data.SONo,
            data.Designation,
            data.ReturnedBy,
            data.PickBy,
            data.DocRemarks,
            data.CheckedBy,
            data.NotedBy,
            data.ApprovedBy);

        await SLActions.PostAsync<object, SalesReturnPayload>("Returns", payload);

        return true;
    }

    public async Task<bool> PostSalesReturnFromDeliveryAsync(SalesReturnDTO data)
    {
        List<object> payloadLines = [];

        // BaseType 15 = A/R Delivery
        foreach (SalesReturnLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new SalesReturnLinesPayload(
                data.DeliveryDocEntry,
                15,
                line.BaseLine,
                data.DocumentLines.IndexOf(line),
                line.ItemCode,
                line.UoMCode,
                line.Quantity,
                line.Warehouse?.WhsCode ?? string.Empty));

        SalesReturnPayload payload = new(
            data.DocDate,
            data.DocDueDate,
            data.BusinessPartner.CardCode,
            data.PreparedBy,
            payloadLines,
            data.ReturnType,
            data.SchoolYear,
            data.DRNo,
            data.SINo,
            data.PURNo,
            data.SONo,
            data.Designation,
            data.ReturnedBy,
            data.PickBy,
            data.DocRemarks,
            data.CheckedBy,
            data.NotedBy,
            data.ApprovedBy);

        await SLActions.PostAsync<object, SalesReturnPayload>("Returns", payload);

        return true;
    }

    public async Task<bool> PostSalesReturnFromRequestAsync(SalesReturnDTO data)
    {
        List<object> payloadLines = [];

        // BaseType for Sales Return Request — update with the correct SAP object type when known
        foreach (SalesReturnLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0))
            payloadLines.Add(new SalesReturnLinesPayload(
                data.SalesReturnRequestDocEntry,
                data.SapReference.BaseEntry,
                line.BaseLine,
                data.DocumentLines.IndexOf(line),
                line.ItemCode,
                line.UoMCode,
                line.Quantity,
                line.Warehouse?.WhsCode ?? string.Empty));

        SalesReturnPayload payload = new(
            data.DocDate,
            data.DocDueDate,
            data.BusinessPartner.CardCode,
            data.PreparedBy,
            payloadLines,
            data.ReturnType,
            data.SchoolYear,
            data.DRNo,
            data.SINo,
            data.PURNo,
            data.SONo,
            data.Designation,
            data.ReturnedBy,
            data.PickBy,
            data.DocRemarks,
            data.CheckedBy,
            data.NotedBy,
            data.ApprovedBy);

        await SLActions.PostAsync<object, SalesReturnPayload>("Returns", payload);

        return true;
    }
}
