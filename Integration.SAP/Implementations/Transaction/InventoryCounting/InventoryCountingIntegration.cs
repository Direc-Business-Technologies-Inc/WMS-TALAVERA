using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Repositories.Integration.Transaction.InventoryCounting;
using Database.Libraries.Repositories;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.InventoryCounting;

namespace Integration.SAP.Implementations.Transaction.InventoryCounting;

public class InventoryCountingIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : IInventoryCountingIntegration
{
    public async Task<bool> PostInventoryCountings(InventoryCountingDocumentDTO data)
    {
        List<InventoryCountingsLinesPayload> payloadLines = [];

        foreach (InventoryCountingDocumentLineDTO line in data.DocumentLines)
            payloadLines.Add(new(line.ItemCode, data.Warehouse.WhsCode, line.UoMCode, line.ActualQuantity));

        InventoryCountingsPayload payload = new(data.CountingDate,
                                                data.AppDocNum.Value,
                                                data.Remarks ?? "WMS User",
                                                payloadLines);

        await SLActions.PostAsync<object, InventoryCountingsPayload>("InventoryCountings", payload);

        return true;
    }
}
