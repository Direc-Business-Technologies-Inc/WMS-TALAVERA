using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Commands.Transaction.Receiving;
using Application.UseCases.Queries.Transaction.Receiving;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.ItemReceipt;
using Web.BlazorServer.ViewModels.Transaction.Packing.ItemReceipt;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.ItemReceipt;

public class ItemReceiptPackingHandler(ISender sender) : IItemReceiptPackingHandler
{
    public async Task<ItemReceiptPackingVM?> GetItemReceiptSourceAsync(string docEntry)
    {
        var dto = await sender.Send(new GetItemReceiptSourceQry(docEntry));
        if (dto is null) return null;

        return MapToVm(dto);
    }

    public async Task<bool> PostItemReceipt(ItemReceiptPackingVM data)
    {
        var dto = MapToDto(data);
        return await sender.Send(new CreateItemReceiptCmd(dto));
    }

    private static ItemReceiptPackingVM MapToVm(ItemReceiptDTO dto)
    {
        return new()
        {
            SourceType = dto.Type.ToLowerInvariant() switch
            {
                "trnfrord" => ItemReceiptPackingVM.SourceTypes.TransferOrder,
                "purchord" => ItemReceiptPackingVM.SourceTypes.PurchaseOrder,
                _ => ItemReceiptPackingVM.SourceTypes.Returns
            },
            CreatedFrom = dto.CreatedFrom,
            Department = dto.Department,
            Vendor = dto.Vendor,
            ReceivedBy = dto.ReceivedBy,
            Location = dto.Location,
            TransferLocation = dto.TransferLocation,
            Subsidiary = dto.Subsidiary,
            ToSubsidiary = dto.ToSubsidiary,
            Date = DateTime.Now,
            SourceInternalId = dto.SourceInternalId,
            DefaultBO = dto.DefaultBO,
            VendorPrefferedBin = dto.VendorPrefferedBin,
            Lines = [.. dto.Lines.Select(MapToVm)]
        };
    }

    private static ItemReceiptLinePackingVM MapToVm(ItemReceiptLineDTO dto)
    {
        return new()
        {
            IsReceived = dto.IsReceived,
            IsLocationBinUsed = dto.IsLocationBinUsed,
            LineNumber = dto.LineNumber,
            PrefferedBinAssignmentId = dto.PrefferedBinAssignmentId,
            ItemCode = dto.ItemCode,
            ItemDescription = dto.ItemDescription,
            UoM = dto.UoM,
            Department = dto.Department,
            Location = dto.Location,
            //WeightActual = dto.WeightActual,
            //WeightRecord = dto.WeightRecord,
            QuantityPlanned = dto.QuantityPlanned,
            QuantityOpen = dto.QuantityOpen,
            QuantityReceived = dto.QuantityReceived,
            QuantityBad = dto.QuantityBad,
            QuantityGood = dto.QuantityGood
        };
    }

    private static ItemReceiptDTO MapToDto(ItemReceiptPackingVM vm)
    {
        return new()
        {
            SourceType = vm.SourceType switch
            {
                ItemReceiptPackingVM.SourceTypes.PurchaseOrder => ItemReceiptDTO.SourceTypes.PurchaseOrder,
                ItemReceiptPackingVM.SourceTypes.Returns => ItemReceiptDTO.SourceTypes.Returns,
                ItemReceiptPackingVM.SourceTypes.TransferOrder => ItemReceiptDTO.SourceTypes.TransferOrder,
                _ => throw new NotImplementedException()
            },
            SourceInternalId = vm.SourceInternalId,
            VendorPrefferedBin = vm.VendorPrefferedBin,
            DefaultBO = vm.DefaultBO,
            CreatedFrom = vm.CreatedFrom,
            Department = vm.Department,
            Vendor = vm.Vendor,
            ReceivedBy = vm.ReceivedBy,
            Location = vm.Location,
            TransferLocation = vm.TransferLocation,
            Subsidiary = vm.Subsidiary,
            ToSubsidiary = vm.ToSubsidiary,
            Date = vm.Date,
            Lines = [.. vm.Lines.Select(MapToDto)]
        };
    }

    private static ItemReceiptLineDTO MapToDto(ItemReceiptLinePackingVM vm)
    {
        return new()
        {
            IsReceived = vm.IsReceived,
            IsLocationBinUsed = vm.IsLocationBinUsed,
            LineNumber = vm.LineNumber,
            PrefferedBinAssignmentId = vm.PrefferedBinAssignmentId,
            ItemCode = vm.ItemCode,
            ItemDescription = vm.ItemDescription,
            UoM = vm.UoM,
            Department = vm.Department,
            Location = vm.Location,
            //WeightActual = vm.WeightActual,
            //WeightRecord = vm.WeightRecord,
            QuantityPlanned = vm.QuantityPlanned,
            QuantityOpen = vm.QuantityOpen,
            QuantityReceived = vm.QuantityReceived,
            QuantityBad = vm.QuantityBad,
            QuantityGood = vm.QuantityGood
        };
    }
}
