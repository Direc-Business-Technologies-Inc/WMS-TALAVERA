using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Commands.Transaction.Receiving;
using Application.UseCases.Queries.Transaction.Receiving;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.Returns;

public class ReturnsItemReceiptPackingHandler(ISender sender) : IReturnsItemReceiptPackingHandler
{
    public async Task<ReturnsItemReceiptPackingVM?> GetItemReceiptSourceAsync(string docEntry)
    {
        var dto = await sender.Send(new GetItemReceiptSourceQry(docEntry));
        if (dto is null) return null;

        return MapToVm(dto);
    }

    public async Task<bool> PostItemReceipt(ReturnsItemReceiptPackingVM data)
    {
        var dto = MapToDto(data);
        return await sender.Send(new CreateItemReceiptCmd(dto));
    }

    private static ReturnsItemReceiptPackingVM MapToVm(ItemReceiptDTO dto)
    {
        return new()
        {
            SourceType = dto.Type.ToLowerInvariant() switch
            {
                "trnfrord" => ReturnsItemReceiptPackingVM.SourceTypes.TransferOrder,
                "purchord" => ReturnsItemReceiptPackingVM.SourceTypes.PurchaseOrder,
                _ => ReturnsItemReceiptPackingVM.SourceTypes.Returns
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

    private static ReturnsItemReceiptLinePackingVM MapToVm(ItemReceiptLineDTO dto)
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
            WeightActual = dto.WeightActual,
            WeightRecord = dto.WeightRecord,
            QuantityPlanned = dto.QuantityPlanned,
            QuantityOpen = dto.QuantityOpen,
            QuantityReceived = dto.QuantityReceived,
            QuantityBad = dto.QuantityBad,
            QuantityGood = dto.QuantityGood
        };
    }

    private static ItemReceiptDTO MapToDto(ReturnsItemReceiptPackingVM vm)
    {
        return new()
        {
            SourceType = vm.SourceType switch
            {
                ReturnsItemReceiptPackingVM.SourceTypes.PurchaseOrder => ItemReceiptDTO.SourceTypes.PurchaseOrder,
                ReturnsItemReceiptPackingVM.SourceTypes.Returns => ItemReceiptDTO.SourceTypes.Returns,
                ReturnsItemReceiptPackingVM.SourceTypes.TransferOrder => ItemReceiptDTO.SourceTypes.TransferOrder,
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

    private static ItemReceiptLineDTO MapToDto(ReturnsItemReceiptLinePackingVM vm)
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
            WeightActual = vm.WeightActual,
            WeightRecord = vm.WeightRecord,
            QuantityPlanned = vm.QuantityPlanned,
            QuantityOpen = vm.QuantityOpen,
            QuantityReceived = vm.QuantityReceived,
            QuantityBad = vm.QuantityBad,
            QuantityGood = vm.QuantityGood
        };
    }
}
