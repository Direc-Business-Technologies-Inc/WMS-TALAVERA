using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;
using Shared.Kernel;
using System.Text.Json;

namespace Integration.SAP.Implementations.Transaction.Receiving;

public class ReceivingIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IReceivingIntegration
{
    public async Task<PurchaseDeliveryNoteHeaderSAPDTO?> GetPurchaseDeliveryNoteHeaderAsync(int docEntry)
    {
        PurchaseDeliveryNoteHeaderSAPDTO? doc = await SLActions.SingleAsync<PurchaseDeliveryNoteHeaderSAPDTO, object>("APHI_Receiving_GoodsReceiptPOHeader", new { docEntry });

        return doc;
    }

    public async Task<IEnumerable<PurchaseDeliveryNoteLineSAPDTO>> GetPurchaseDeliveryNoteLinesAsync(int docEntry)
    {
        List<PurchaseDeliveryNoteLineSAPDTO>? lines = await SLActions.QueryAsync<PurchaseDeliveryNoteLineSAPDTO, object>("APHI_Receiving_GoodsReceiptPOLines", new { docEntry });

        return lines;
    }

    public async Task<(IEnumerable<PurchaseDeliveryNoteSAPDTO>, int)> GetPurchaseDeliveryNotesListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "BaseDocEntry", "T1.BaseEntry" },
                { "BaseDocNum", "T3.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "DocDueDate", "T0.DocDueDate" },
                { "CardCode", "T0.CardCode" },
                { "CardName", "T5.CardName" },
                { "SupplierContactPerson", "T6.Name" },
                { "WhsCode", "T1.WhsCode" },
                { "WhsName", "T4.WhsName" },
                { "ReceivedBy", "T0.U_RecBy" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Receiving_GoodsReceiptPOs", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all open Purchase Orders not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<PurchaseDeliveryNoteSAPDTO> docs = await SLActions.RawQueryAsync<PurchaseDeliveryNoteSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<PurchaseOrderHeaderSAPDTO?> GetPurchaseOrderHeaderAsync(int docEntry)
    {
        PurchaseOrderHeaderSAPDTO? doc = await SLActions.SingleAsync<PurchaseOrderHeaderSAPDTO, object>("APHI_Receiving_OpenPurchaseOrderHeader", new { docEntry });

        return doc;
    }

    public async Task<IEnumerable<PurchaseOrderLineSAPDTO>> GetPurchaseOrderLinesAsync(int docEntry)
    {
        List<PurchaseOrderLineSAPDTO>? lines = await SLActions.QueryAsync<PurchaseOrderLineSAPDTO, object>("APHI_Receiving_OpenPurchaseOrderLines", new { docEntry });

        return lines;
    }

    public async Task<(IEnumerable<PurchaseOrderSAPDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "DocDueDate", "T0.DocDueDate" },
                { "CardCode", "T0.CardCode" },
                { "CardName", "C0.CardName" },
                { "SupplierContactPerson", "P0.Name" },
                { "Remarks", "T0.Remarks" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Receiving_OpenPurchaseOrders", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all open Purchase Orders not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<PurchaseOrderSAPDTO> docs = await SLActions.RawQueryAsync<PurchaseOrderSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    public async Task<IEnumerable<PurchaseTypeSAPDTO>> GetPurchaseTypesAsync()
    {
        List<PurchaseTypeSAPDTO>? lines = await SLActions.QueryAsync<PurchaseTypeSAPDTO>("APHI_Receiving_PurchaseType");

        return lines;
    }

    public async Task<bool> PostGoodsReceiptPOAsync(PurchaseDeliveryNoteDTO data)
    {
        List<object> payloadLines = [];

        foreach (PurchaseDeliveryNoteLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0 && dl.Price > 0))
            payloadLines.Add(new GoodsReceiptPOLinesPayload(line.BaseEntry,
                                 line.BaseEntry == 0 ? 0 : 22,
                                 line.BaseLine,
                                 data.DocumentLines.Where(dl => dl.Quantity > 0).ToList().IndexOf(line),
                                 line.ItemCode,
                                 line.Quantity,
                                 line.Price,
                                 line.TaxCode,
                                 line.Warehouse.WhsCode,
                                 EnumHelper.GetEnumDescription(line.InputType)));

        foreach (PurchaseDeliveryNoteLineDTO line in data.DocumentLines.Where(dl => dl.Quantity > 0 && dl.Price <= 0))
            payloadLines.Add(new GoodsReceiptPOLinesStandalonePayload(data.DocumentLines.Where(dl => dl.Quantity > 0).ToList().IndexOf(line),
                                 line.ItemCode,
                                 line.Quantity,
                                 line.Price,
                                 line.TaxCode,
                                 line.Warehouse.WhsCode,
                                 EnumHelper.GetEnumDescription(line.InputType)));


        GoodsReceiptPOPayload payload = new(data.BusinessPartner.CardCode,
                                            data.DocDate,
                                            data.DocDueDate,
                                            data.DocDate,
                                            data.PreparedBy,
                                            payloadLines,
                                            data.SchoolYear,
                                            data.PONo,
                                            data.DRNo,
                                            data.Time,
                                            data.SINo,
                                            data.PurchaseType,
                                            data.ItemName,
                                            data.DeliveredBy,
                                            data.ReceivedBy,
                                            data.DocRemarks,
                                            data.ReviewedBy,
                                            data.ApprovedBy,
                                            data.NotedBy);

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await SLActions.PostAsync<object, GoodsReceiptPOPayload>("PurchaseDeliveryNotes", payload);

        return true;
    }
}
