using Application.DataTransferObjects.Others.SAP;
using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Commands.Transaction.InventoryCounting;
using Application.UseCases.Queries.Transaction.InventoryCounting;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryCounting;

public class InventoryCountingHandler(ISender Sender) : IInventoryCountingHandler
{
    public async Task<(IEnumerable<InventoryCountingDataGridVM> Data, int Count)> GetInventoryCountingDataGridAsync(DataGridIntent intent)
    {
        GetInventoryCountingDataGridQry qry = new(intent);
        (IEnumerable<InventoryCountingDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<InventoryCountingDataGridVM>>(), Count);
    }

    public async Task<InventoryCountingVM?> GetInventoryCountingDocumentAsync(Guid id)
    {
        GetInventoryCountingDocumentQry qry = new(id);
        InventoryCountingDocumentDTO? response = await Sender.Send(qry);

        return response.Adapt<InventoryCountingVM?>();
    }

    public async Task<bool> CreateInventoryCountingDocumentAsync(InventoryCountingVM data)
    {
        CreateInventoryCountingDocumentCmd cmd = new(data.Adapt<InventoryCountingDocumentDTO>());
        return await Sender.Send(cmd);
    }

    public async Task<bool> SaveInventoryCountingDocumentAsync(Guid id)
    {
        SaveInventoryCountingDocumentCmd cmd = new(id);
        return await Sender.Send(cmd);
    }

    public async Task<bool> PostInventoryCountingDocumentAsync(Guid id)
    {
        PostInventoryCountingDocumentCmd cmd = new(id);
        return await Sender.Send(cmd);
    }

    public async Task<bool> RecountInventoryCountingDocumentAsync(Guid id)
    {
        RecountInventoryCountingDocumentCmd cmd = new(id);
        return await Sender.Send(cmd);
    }

    public async Task<bool> CreateInventoryCountingSheetAsync(InventoryCountingSheetVM sheet)
    {
        CreateInventoryCountingSheetCmd cmd = new(sheet.Adapt<InventoryCountingSheetDTO>());
        return await Sender.Send(cmd);
    }

    public async Task<bool> IgnoreInventoryCountingSheetAsync(Guid documentId, string sheetNo)
    {
        IgnoreInventoryCountingSheetCmd cmd = new(documentId, sheetNo);
        return await Sender.Send(cmd);
    }

    public async Task<bool> SyncInventoryCountingSheetAsync(Guid documentId, string sheetNo)
    {
        SyncInventoryCountingSheetCmd cmd = new(documentId, sheetNo);
        return await Sender.Send(cmd);
    }

    public async Task<IEnumerable<InventoryCountingLineVM>> GetWarehouseItemsForCountingAsync(string whsCode)
    {
        GetWarehouseItemsForCountingQry qry = new(whsCode);
        IEnumerable<InventoryCountingItemSAPDTO> response = await Sender.Send(qry);

        return response.Adapt<IEnumerable<InventoryCountingLineVM>>();
    }
}
