using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Commands.Transaction.Receiving;
using Application.UseCases.Queries.Transaction.Receiving;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Receiving;

public class ReceivingHandler(
    ISender Sender) 
    : IReceivingHandler
{
    public async Task<PurchaseOrderVM?> GetPurchaseOrderAsync(string docEntry)
    {
        GetPurchaseOrderQry qry = new(docEntry);
        PurchaseOrderDTO? response = await Sender.Send(qry);
        PurchaseOrderVM vm = new();
        if (response is not null)
        {
            response.Adapt(vm);
            vm.DocumentLines = [..response.Lines.Adapt<IEnumerable<PurchaseOrderLineVM>>()];
        }

        return vm;
    }

    public async Task<(IEnumerable<PurchaseOrderDataGridVM> Data, int Count)> GetPurchaseOrderDataGridAsync(DataGridIntent intent)
    {
        GetPurchaseOrdersQry qry = new(intent);
        (IEnumerable<PurchaseOrderDataGridDTO> Data, int Count) = await Sender.Send(qry);

        var x = Data.Select(x => new PurchaseOrderDataGridVM
        {
            Id = x.Id,
            ReferenceNumber = x.ReferenceNumber,
            Date = x.Date,
            DeliveryDate = x.DeliveryDate,
            Vendor = x.VendorName,
            Remarks = x.Memo
        });

        return (x, Count);
    }
    public async Task<(IEnumerable<ReturnsDataGridVM> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent)
    {
        GetReturnsListQry qry = new (intent);
        (var data, var count) = await Sender.Send(qry);
        var x = data.Select(x => new ReturnsDataGridVM
        {
            ReferenceNumber = x.ReferenceNumber,
            Date = x.Date,
            FromSubsidiary = x.SourceSubsidiary,
            ToSubsidiary = x.DestinationSubsidiary,
            Vendor = x.VendorName,
            SourceWarehouse = x.Location,
            DestinationWarehouse = x.TransferLocation,
            Remarks = x.Memo
        });

        return (x, count);
    }


    public async Task<(IEnumerable<TransferOrderDataGridVM> Data, int Count)> GetTransferOrderDataGridAsync(DataGridIntent intent)
    {
        GetTransferOrdersQry qry = new(intent);
        (IEnumerable<TransferOrderDataGridDTO> Data, int Count) = await Sender.Send(qry);

        var x = Data.Select(x => new TransferOrderDataGridVM
        {
            Id = x.Id,
            ReferenceNumber = x.ReferenceNumber,
            Date = x.Date,
            Location = x.Location,
            TransferLocation = x.TransferLocation,
            SourceSubsidiary = x.SourceSubsidiary,
            DesctinationSubsidiary = x.DestinationSubsidiary, 
        });

        return (x, Count);
    }

    public async Task<ReturnsVM?> GetReturnsAsync(string docEntry)
    {
        GetReturnsQry query = new(docEntry);

        var x = await Sender.Send(query);
        if (x is null) return null;

        return new ReturnsVM()
        {
            ReferenceNumber = x.ReferenceNumber,
            FromSubsidiary = x.FromSubsidiary,
            Vendor = x.Vendor,
            FromWarehouse = x.FromWarehouse,
            ToWarehouse = x.ToWarehouse,
            PreparedBy = x.PreparedBy,
            ReceivedBy = x.ReceivedBy,
            Date = x.Date,
            Lines = x.Lines.Adapt<List<ReturnsLineVM>>()
        };
    }


    public async Task<bool> PostGoodsReceiptPOAsync(PurchaseOrderVM data)
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<TransferOrderLineVM> Data, int Count)> GetTransferOrderLinesDataGridAsync(string transferOrderId, DataGridIntent intent)
    {
        GetTransferOrderLinesQry qry = new(transferOrderId, intent);

        var (rawData, count) = await Sender.Send(qry);

        return (rawData.Adapt<IEnumerable<TransferOrderLineVM>>(), count);
    }


    public async Task<TransferOrderVM?> GetTransferOrderAsync(string transferOrderId)
    {
        GetTransferOrderInfoQry qry = new(transferOrderId);
        TransferOrderDTO? dto = await Sender.Send(qry);
        if (dto is null) return null;

        return new TransferOrderVM()
        {
            Id = dto.Id,
            Date = dto.Date,
            ReferenceNumber = dto.ReferenceNumber,
            FromSubsidiary = dto.FromSubsidiary,
            ToSubsidiary = dto.ToSubsidiary,
            SourceWarehouse = dto.Location,
            DestinationWarehouse = dto.TransferLocation,
            PreparedBy = dto.PreparedBy,
            ReceivedBy = dto.ReceivedBy,
        };
    }

    public async Task<ItemReceiptVM?> GetItemReceiptSourceAsync(string docEntry)
    {
        GetItemReceiptSourceQry query = new(docEntry);

        var x = await Sender.Send(query);
        if (x is null) return null;

        var result = x.Adapt<ItemReceiptVM>();
        result.SourceType = x.Type.ToLowerInvariant() switch
        {
            "trnfrord" => ItemReceiptVM.SourceTypes.TransferOrder,
            "purchord" => ItemReceiptVM.SourceTypes.PurchaseOrder,
            _ => ItemReceiptVM.SourceTypes.Returns
        };

        result.Date = DateTime.Now;

        return result;
    }

    public async Task<bool> PostItemReceipt(ItemReceiptVM data)
    {
        var dto = new ItemReceiptDTO();

        data.Adapt(dto);
        dto.SourceType = data.SourceType switch
        {
            ItemReceiptVM.SourceTypes.PurchaseOrder => ItemReceiptDTO.SourceTypes.PurchaseOrder,
            ItemReceiptVM.SourceTypes.Returns => ItemReceiptDTO.SourceTypes.Returns,
            ItemReceiptVM.SourceTypes.TransferOrder => ItemReceiptDTO.SourceTypes.TransferOrder,
            _ => throw new NotImplementedException(),
        };

        var cmd = new CreateItemReceiptCmd(dto);
        return await Sender.Send(cmd);
    }
}