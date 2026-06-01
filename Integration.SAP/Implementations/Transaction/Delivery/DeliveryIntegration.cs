using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.Delivery;
using Shared.Entities;
using System.Text.Json;

namespace Integration.SAP.Implementations.Transaction.Delivery;

public class DeliveryIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IDeliveryIntegration
{
    public async Task<DeliveryHeaderSAPDTO?> GetDeliveryDocumentHeaderAsync(int docEntry)
    {
        DeliveryHeaderSAPDTO? doc = await SLActions.SingleAsync<DeliveryHeaderSAPDTO, object>("APHI_Delivery_Header", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<IEnumerable<DeliveryLineSAPDTO>> GetDeliveryDocumentLinesAsync(int docEntry)
    {
        IEnumerable<DeliveryLineSAPDTO> doc = await SLActions.QueryAsync<DeliveryLineSAPDTO, object>("APHI_Delivery_Lines", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<(IEnumerable<DeliveryDataGridSAPDTO> Data, int Count)> GetDeliveryDocumentsAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "ODLN.DocEntry" },
                { "DocNum", "ODLN.DocNum" },
                { "DocDate", "ODLN.DocDate" },
                { "PreparedBy", "ODLN.U_PrepBy" },
                { "CardCode", "ODLN.CardCode" },
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

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Delivery_DataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all Approved Deliveries not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<DeliveryDataGridSAPDTO> docs = await SLActions.RawQueryAsync<DeliveryDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<IEnumerable<DeliveryMeansSAPDTO>> GetDeliveryMeansAsync()
    {
        IEnumerable<DeliveryMeansSAPDTO> data = await SLActions.QueryAsync<DeliveryMeansSAPDTO>("APHI_Delivery_DeliveryMeans");

        return data;
    }

    public async Task<SalesOrderHeaderSAPDTO?> GetSalesOrderDocumentHeaderAsync(int docEntry)
    {
        SalesOrderHeaderSAPDTO? doc = await SLActions.SingleAsync<SalesOrderHeaderSAPDTO, object>("APHI_SalesOrder_Header", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<IEnumerable<SalesOrderLineSAPDTO>> GetSalesOrderDocumentLinesAsync(int docEntry)
    {
        IEnumerable<SalesOrderLineSAPDTO> doc = await SLActions.QueryAsync<SalesOrderLineSAPDTO, object>("APHI_SalesOrder_Lines", new { DocEntry = docEntry });

        return doc;
    }

    public async Task<(IEnumerable<SalesOrderDataGridSAPDTO> Data, int Count)> GetSalesOrderDocumentsAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "ORDR.DocEntry" },
                { "DocNum", "ORDR.DocNum" },
                { "DocDate", "ORDR.DocDate" },
                { "PreparedBy", "ORDR.U_PrepBy" },
                { "CardCode", "ORDR.CardCode" },
                { "CardName", "OCRD.CardName" },
                { "ContactPerson", "ORDR.CntctPrsn" },
                { "DocRemarks", "ORDR.U_Remarks" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocEntry",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_SalesOrder_DataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all open Sales Orders not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<SalesOrderDataGridSAPDTO> docs = await SLActions.RawQueryAsync<SalesOrderDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<bool> PostDeliveryDocument(DeliveryDTO document)
    {
        List<DeliveryNotesLinesPayload> payloadLines = [];
        List<DeliveryLineDTO> validLines = [.. document.DocumentLines.Where(dl => dl.Quantity > 0)];

        for (int i = 0; i < validLines.Count; i++)
        {
            var line = validLines[i];

            payloadLines.Add(new DeliveryNotesLinesPayload(
                document.SapReference.BaseEntry,
                17,
                line.LineNum,
                i,
                line.ItemCode,
                line.Quantity,
                line.Warehouse.WhsCode));
        }

        DateTime? actualDelivDate = null;
        if (DateTime.TryParse(document.ActualDelivDate, out DateTime parsedDate))
            actualDelivDate = parsedDate;

        DeliveryNotesPayload payload = new(
            document.BusinessPartner.CardCode,
            document.DocDate,
            document.DocDueDate,
            document.PreparedBy,
            payloadLines,
            document.SchoolYear,
            document.DRNo,
            document.DeliveryMeans,
            document.Courier,
            document.CourierName,
            actualDelivDate,
            document.Designation,
            document.WayBillNo,
            document.PlateNo,
            document.Driver,
            document.DocRemarks,
            document.ReceivedBy,
            document.ApprovedBy,
            document.NotedBy);

        string jsonString = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await SLActions.PostAsync<object, DeliveryNotesPayload>("DeliveryNotes", payload);

        return true;
    }
}
