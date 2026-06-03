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
        ReceivingDTO? response = await Sender.Send(qry);
        PurchaseOrderVM vm = new();
        if (response is not null)
        {
            response.DocumentInfo.Adapt(vm);
            vm.DocumentLines = [..response.DocumentLines.Adapt<IEnumerable<PurchaseOrderLineVM>>()];
        }

        return vm;
    }

    public async Task<(IEnumerable<ReceivingPurchaseOrderDataGridVM> Data, int Count)> GetPurchaseOrderDataGridAsync(DataGridIntent intent)
    {
        GetPurchaseOrdersQry qry = new(intent);
        (IEnumerable<ReceivingDataGridDTO> Data, int Count) = await Sender.Send(qry);

        var x = Data.Select(x => new ReceivingPurchaseOrderDataGridVM
        {
            Id = x.Id,
            ReferenceNumber = x.ReferenceNumber,
            Date = x.Date,
            Vendor = x.SourceSubsidiary,
            Remarks = x.Memo
        });

        return (x, Count);
    }

    public async Task<(IEnumerable<ReceivingTransferOrderDataGridVM> Data, int Count)> GetTransferOrderDataGridAsync(DataGridIntent intent)
    {
        GetTransferOrdersQry qry = new(intent);
        (IEnumerable<ReceivingDataGridDTO> Data, int Count) = await Sender.Send(qry);

        var x = Data.Select(x => new ReceivingTransferOrderDataGridVM
        {
            Id = x.Id,
            ReferenceNumber = x.ReferenceNumber,
            Date = x.Date,
            SourceLocation = x.Location,
            TransferLocation = x.TransferLocation
        });

        return (x, Count);
    }

    public async Task<bool> PostGoodsReceiptPOAsync(PurchaseOrderVM data)
    {
        PostGoodsReceiptPOCmd cmd = new(data.Adapt<ReceivingDTO>());
        bool result = await Sender.Send(cmd);

        return result;
    }

    public async Task<IEnumerable<PurchaseTypeVM>> GetPurchaseTypesAsync()
    {
        GetPurchaseTypesQry qry = new();
        IEnumerable<PurchaseTypeDTO> response = await Sender.Send(qry);

        return response.Adapt<IEnumerable<PurchaseTypeVM>>();
    }

    public async Task<(IEnumerable<TransferOrderLineVM> Data, int Count)> GetTransferOrderLinesDataGridAsync(int transferOrderId, DataGridIntent intent)
    {
        GetTransferOrderLinesQry qry = new(transferOrderId, intent);

        var (rawData, count) = await Sender.Send(qry);

        return (rawData.Adapt<IEnumerable<TransferOrderLineVM>>(), count);
    }


    public async Task<TransferOrderVM?> GetTransferOrderAsync(int transferOrderId)
    {
        GetTransferOrderInfoQry qry = new(transferOrderId);
        var dto = await Sender.Send(qry);
        if (dto is null) return null;

        return new TransferOrderVM()
        {
            Id = dto.Id,
            ReferenceNumber = dto.ReferenceNumber,
            Date = dto.Date,
            DeliveryDate = dto.DeliveryDate,
            RequestorName = dto.RequestorName,
            SourceLocation = dto.Location,
            DestinationLocation = dto.TransferLocation,
            Status = dto.Status,
        };
    }

}
