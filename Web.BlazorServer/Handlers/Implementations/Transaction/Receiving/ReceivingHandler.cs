using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Commands.Transaction.Receiving;
using Application.UseCases.Queries.Transaction.Receiving;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Receiving;

public class ReceivingHandler(
    ISender Sender) 
    : IReceivingHandler
{
    public async Task<(IEnumerable<PurchaseDeliveryNoteDataGridVM> Data, int Count)> GetPurchaseDeliveryNoteDataGridAsync(DataGridIntent intent)
    {
        GetPurchaseDeliveryNotesQry qry = new(intent);
        (IEnumerable<PurchaseDeliveryNoteDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<PurchaseDeliveryNoteDataGridVM>>(), Count);
    }

    public async Task<PurchaseDeliveryNoteVM?> GetPurchaseDeliveryNoteAsync(int docEntry)
    {
        GetPuchaseDeliveryNoteQry qry = new(docEntry);
        PurchaseDeliveryNoteDTO? response = await Sender.Send(qry);

        return response.Adapt<PurchaseDeliveryNoteVM?>();
    }

    public async Task<PurchaseOrderVM?> GetPurchaseOrderAsync(int docEntry)
    {
        GetPurchaseOrderQry qry = new(docEntry);
        PurchaseOrderDTO? response = await Sender.Send(qry);

        return response.Adapt<PurchaseOrderVM?>();
    }

    public async Task<(IEnumerable<PurchaseOrderDataGridVM> Data, int Count)> GetPurchaseOrderDataGridAsync(DataGridIntent intent)
    {
        GetPurchaseOrdersQry qry = new(intent);
        (IEnumerable<PurchaseOrderDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<PurchaseOrderDataGridVM>>(), Count);
    }

    public async Task<bool> PostGoodsReceiptPOAsync(PurchaseOrderVM data)
    {
        PostGoodsReceiptPOCmd cmd = new(data.Adapt<PurchaseOrderDTO>());
        bool result = await Sender.Send(cmd);

        return result;
    }

    public async Task<IEnumerable<PurchaseTypeVM>> GetPurchaseTypesAsync()
    {
        GetPurchaseTypesQry qry = new();
        IEnumerable<PurchaseTypeDTO> response = await Sender.Send(qry);

        return response.Adapt<IEnumerable<PurchaseTypeVM>>();
    }
}
