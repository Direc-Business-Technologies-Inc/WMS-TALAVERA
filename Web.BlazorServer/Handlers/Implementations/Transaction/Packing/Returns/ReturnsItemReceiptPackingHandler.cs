using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Commands.Transaction.Packing.NS.Returns;
using Application.UseCases.Queries.Transaction.Packing.NS.Returns;
using Application.UseCases.Queries.Transaction.Packing.Returns;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.Returns;

public class ReturnsItemReceiptPackingHandler(ISender sender) : IReturnsItemReceiptPackingHandler
{
    public async Task<ReturnsItemReceiptPackingVM?> GetItemReceiptSourceAsync(string docEntry)
    {
        var header = await sender.Send(new GetPackingReturnQry(docEntry));
        if (header is null) return null;

        var lines = (await sender.Send(new GetReturnsLineQry(new ReturnsLineRequestDTO
        {
            OrderNumber = docEntry
        }))).ToList();

        return new()
        {
            SourceType = ReturnsItemReceiptPackingVM.SourceTypes.Returns,
            CreatedFrom = header.ReferenceNumber,
            Department = "Operations",
            TransferCategory = header.TransferCategory,
            ReceivedBy = header.ReceivedBy,
            Location = header.Location,
            TransferLocation = header.TransferLocation,
            Subsidiary = header.FromSubsidiary,
            ToSubsidiary = header.ToSubsidiary,
            Date = header.Date,
            SourceInternalId = lines.FirstOrDefault()?.NetsuiteOrderInternalId ?? header.Id,
            Lines = [.. lines.Select(MapToVm)]
        };
    }

    public async Task<bool> PostItemFulfillment(ReturnsItemReceiptPackingVM data)
    {
        var sourceLines = (await sender.Send(new GetReturnsLineQry(new ReturnsLineRequestDTO
        {
            OrderNumber = data.CreatedFrom
        }))).ToList();

        var submittedLines = data.Lines.ToDictionary(line => line.LineNumber);
        var dto = sourceLines
            .Select(line =>
            {
                submittedLines.TryGetValue(line.LineSequenceNumber, out var submittedLine);
                return MapToDto(line, submittedLine);
            })
            .ToList();

        if (!dto.Any(line => line.ScannedQuantity > 0))
        {
            throw new InvalidOperationException("Please enter at least one quantity to fulfill.");
        }

        var result = await sender.Send(new PostReturnsIFCmd(dto));
        if (!result.Success)
        {
            throw new Exception(result.ErrorMessage);
        }

        return result.Data == true;
    }

    private static ReturnsItemReceiptLinePackingVM MapToVm(ReturnsLineDTO dto)
    {
        var quantityPlanned = ConvertQuantity(dto.LineQuantity, dto.UoMRate);
        var quantityOpen = GetRemainingQuantity(dto.LineQuantity, dto.LineQuantityBackOrdered, dto.LineQuantityPacked, dto.UoMRate);
        var quantityPacked = ConvertQuantity(dto.LineQuantityPacked, dto.UoMRate);
        var quantityBackOrdered = ConvertQuantity(dto.LineQuantityBackOrdered, dto.UoMRate);

        return new()
        {
            IsReceived = true,
            IsLocationBinUsed = IsNetSuiteTrue(dto.LocationUsedBin),
            LineNumber = dto.LineSequenceNumber,
            PrefferedBinAssignmentId = dto.NetsuiteMaterialPrefferedBinId,
            VendorAssignedBinId = dto.NetsuiteMaterialVendorAssignedBin,
            ItemCode = dto.MaterialCode,
            ItemDescription = dto.MaterialName,
            UoM = dto.UoMName,
            Location = dto.LocationName,
            QuantityPlanned = quantityPlanned,
            QuantityBackOrdered = quantityBackOrdered,
            QuantityOpen = quantityOpen,
            QuantityReceived = quantityPacked
        };
    }

    private static PostReturnsDTO MapToDto(ReturnsLineDTO dto, ReturnsItemReceiptLinePackingVM? submittedLine)
    {
        var scannedQuantity = submittedLine is not null && submittedLine.IsReceived
            ? submittedLine.QuantityGood + submittedLine.QuantityBad
            : 0;

        var quantityPlanned = ConvertQuantity(dto.LineQuantity, dto.UoMRate);
        var quantityPacked = ConvertQuantity(dto.LineQuantityPacked, dto.UoMRate);
        var quantityOpen = GetRemainingQuantity(dto.LineQuantity, dto.LineQuantityBackOrdered, dto.LineQuantityPacked, dto.UoMRate);

        return new()
        {
            NetsuiteOrderInternalId = dto.NetsuiteOrderInternalId,
            OrderNumber = dto.OrderNumber,
            OrderType = dto.OrderType,
            OrderStatus = dto.OrderStatus,
            TransferCategory = dto.TransferCategory,
            NetsuiteFromLocationInternalId = dto.NetsuiteFromLocationInternalId,
            NetsuiteToLocationInternalId = dto.NetsuiteToLocationInternalId,
            NetsuiteFromSubsidiaryInternalId = dto.NetsuiteFromSubsidiaryInternalId,
            NetsuiteSubsidiaryDefaultBOInternalId = dto.NetsuiteSubsidiaryDefaultBOInternalId,
            NetsuitePrefferedBadBinId = dto.NetsuitePrefferedBadBinId,
            NetsuiteToSubsidiaryInternalId = dto.NetsuiteToSubsidiaryInternalId,
            NetsuiteSubsidiaryInternalId = dto.NetsuiteSubsidiaryInternalId,
            NetsuiteLocationInternalId = dto.NetsuiteLocationInternalId,
            LocationName = dto.LocationName,
            LocationUsedBin = dto.LocationUsedBin,
            IsLocationUsedBin = IsNetSuiteTrue(dto.LocationUsedBin),
            LineSequenceNumber = dto.LineSequenceNumber,
            TransactionLineType = dto.TransactionLineType,
            NetsuiteMaterialInternalId = dto.NetsuiteMaterialInternalId,
            MaterialCode = dto.MaterialCode,
            MaterialName = dto.MaterialName,
            MaterialWeight = dto.MaterialWeight,
            NetsuiteMaterialPrefferedBinId = dto.NetsuiteMaterialPrefferedBinId,
            NetsuiteMaterialVendorAssignedBin = dto.NetsuiteMaterialVendorAssignedBin,
            LineQuantity = dto.LineQuantity,
            LineQuantityPacked = dto.LineQuantityPacked,
            LineQuantityBackOrdered = dto.LineQuantityBackOrdered,
            NetsuiteUoMInternalId = dto.NetsuiteUoMInternalId,
            UoMName = dto.UoMName,
            UoMRate = dto.UoMRate,
            NetsuiteOrderCreatedDate = dto.NetsuiteOrderCreatedDate,
            NSLineQuantity = quantityPlanned,
            NSLineQuantityPacked = quantityOpen,
            NSLineQuantityShipped = quantityPacked,
            ScanCount = scannedQuantity > 0 ? 1 : 0,
            ScannedQuantity = scannedQuantity,
            ScannedWeight = 0,
            TotalWeight = scannedQuantity * dto.MaterialWeight,
            TotalQuantity = quantityOpen,
            IsBad = submittedLine?.QuantityBad > 0 && submittedLine.QuantityGood == 0,
            AlreadyFulfilled = scannedQuantity == quantityOpen,
            OverScanned = scannedQuantity > quantityOpen
        };
    }

    private static decimal ConvertQuantity(decimal quantity, decimal uoMRate) =>
        uoMRate == 0 ? quantity : quantity / uoMRate;

    private static decimal GetRemainingQuantity(decimal quantity, decimal backOrderedQuantity, decimal fulfilledQuantity, decimal uoMRate) =>
        Math.Max(0, ConvertQuantity(quantity - (backOrderedQuantity + fulfilledQuantity), uoMRate));

    private static bool IsNetSuiteTrue(string value) =>
        value.Equals("T", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("true", StringComparison.OrdinalIgnoreCase);

}
