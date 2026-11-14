using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Packing.NS.Request;
using Application.UseCases.Commands.Transaction.Packing.NS.VendorReturnAuthorization;
using Application.UseCases.Queries.Transaction.Packing.NS.VendorReturnAuthorization;
using Application.UseCases.Queries.Transaction.Packing.VendorReturnAuthorization;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.VendorReturnAuthorization;

public class VendorReturnAuthorizationItemReceiptPackingHandler(ISender sender) : IVendorReturnAuthorizationItemReceiptPackingHandler
{
    public async Task<VendorReturnAuthorizationItemReceiptPackingVM?> GetItemReceiptSourceAsync(string docEntry)
    {
        var header = await sender.Send(new GetPackingVendorReturnAuthorizationQry(docEntry));
        if (header is null) return null;

        var lines = (await sender.Send(new GetVendorReturnAuthorizationLineQry(new VendorReturnAuthorizationLineRequestDTO
        {
            OrderNumber = docEntry
        }))).ToList();

        return new()
        {
            SourceType = VendorReturnAuthorizationItemReceiptPackingVM.SourceTypes.VendorReturnAuthorization,
            CreatedFrom = header.ReferenceNumber,
            Department = "Operations",
            Vendor = lines.FirstOrDefault()?.VendorName ?? string.Empty,
            LocationId = header.LocationId,
            Location = header.Location,
            IsTripTicketExempt = header.IsTripTicketExempt,
            Subsidiary = header.FromSubsidiary,
            Date = header.Date,
            SourceInternalId = lines.FirstOrDefault()?.NetsuiteOrderInternalId ?? header.Id,
            Lines = [.. lines.Select(MapToVm)]
        };
    }

    public async Task<bool> PostItemFulfillment(VendorReturnAuthorizationItemReceiptPackingVM data)
    {
        var sourceLines = (await sender.Send(new GetVendorReturnAuthorizationLineQry(new VendorReturnAuthorizationLineRequestDTO
        {
            OrderNumber = data.CreatedFrom
        }))).ToList();

        var submittedLines = data.Lines.ToDictionary(line => line.LineNumber);
        ValidateInventoryStatusAvailability(sourceLines, submittedLines);

        var dto = sourceLines
            .Where(x => GetRemainingQuantity(x.LineQuantity, x.LineQuantityBackOrdered, x.LineQuantityPacked, x.UoMRate) > 0)
            .SelectMany(line =>
            {
                submittedLines.TryGetValue(line.LineSequenceNumber, out var submittedLine);

                return new[]
                {
                    MapToDto(line, submittedLine, data.IsTripTicketExempt, isBad: false),
                    MapToDto(line, submittedLine, data.IsTripTicketExempt, isBad: true)
                };
            })
            .ToList();

        //if (!dto.Any(line => line.ScannedQuantity > 0))
        //{
        //    throw new InvalidOperationException("Please enter at least one quantity to fulfill.");
        //}

        var result = await sender.Send(new PostVendorReturnAuthorizationIFCmd(dto));
        if (!result.Success)
        {
            throw new Exception(result.ErrorMessage);
        }

        return result.Data == true;
    }

    private static void ValidateInventoryStatusAvailability(
        IEnumerable<VendorReturnAuthorizationLineDTO> sourceLines,
        IReadOnlyDictionary<int, VendorReturnAuthorizationItemReceiptLinePackingVM> submittedLines)
    {
        var unavailable = new List<string>();

        foreach (var line in sourceLines)
        {
            if (!submittedLines.TryGetValue(line.LineSequenceNumber, out var submittedLine) || !submittedLine.IsReceived)
                continue;

            ValidateCondition(line, submittedLine.QuantityGood, isBad: false, unavailable);
            ValidateCondition(line, submittedLine.QuantityBad, isBad: true, unavailable);
        }

        if (unavailable.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot fulfill vendor return because the selected inventory status is unavailable: {string.Join("; ", unavailable)}. " +
                "Nothing was saved. Reload the order and verify the item, source location, bin, and inventory status in NetSuite.");
        }
    }

    private static void ValidateCondition(
        VendorReturnAuthorizationLineDTO line,
        decimal requestedQuantity,
        bool isBad,
        ICollection<string> unavailable)
    {
        if (requestedQuantity <= 0) return;

        if (VendorReturnAuthorizationInventoryAvailability.CanFulfill(line, isBad, requestedQuantity)) return;

        var availableQuantity = VendorReturnAuthorizationInventoryAvailability.GetAvailableQuantity(line, isBad);

        var status = isBad ? "Bad (3)" : "Good (1)";
        var binId = VendorReturnAuthorizationInventoryAvailability.GetSelectedBinId(line);
        var context = binId is null ? $"location {line.LocationName}" : $"bin {binId} at {line.LocationName}";

        unavailable.Add(
            $"line {line.LineSequenceNumber} {line.MaterialCode} - {line.MaterialName}, status {status}, {context}, requested {requestedQuantity}, available {availableQuantity}");
    }

    private static VendorReturnAuthorizationItemReceiptLinePackingVM MapToVm(VendorReturnAuthorizationLineDTO dto)
    {
        var quantityPlanned = ConvertQuantity(dto.LineQuantity, dto.UoMRate);
        var quantityOpen = GetRemainingQuantity(dto.LineQuantity, dto.LineQuantityBackOrdered, dto.LineQuantityPacked, dto.UoMRate);
        var quantityReceived = ConvertQuantity(dto.LineQuantityPacked, dto.UoMRate);

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
            QuantityOnHand = dto.LocationItemQuantityOnHand,
            QuantityPlanned = quantityPlanned,
            QuantityOpen = quantityOpen,
            QuantityReceived = quantityReceived
        };
    }

    private static PostVendorReturnAuthorizationDTO MapToDto(VendorReturnAuthorizationLineDTO dto, VendorReturnAuthorizationItemReceiptLinePackingVM? submittedLine, bool isTripTicketExempt, bool isBad)
    {
        var scannedQuantity = submittedLine is not null && submittedLine.IsReceived
            ? isBad ? submittedLine.QuantityBad : submittedLine.QuantityGood
            : 0;

        var quantityPlanned = ConvertQuantity(dto.LineQuantity, dto.UoMRate);
        var quantityReceived = ConvertQuantity(dto.LineQuantityReceived, dto.UoMRate);
        var quantityOpen = GetRemainingQuantity(dto.LineQuantity, dto.LineQuantityBackOrdered, dto.LineQuantityPacked, dto.UoMRate);

        return new()
        {
            NetsuiteOrderInternalId = dto.NetsuiteOrderInternalId,
            OrderNumber = dto.OrderNumber,
            OrderType = dto.OrderType,
            OrderStatus = dto.OrderStatus,
            IsTripTicketExempt = isTripTicketExempt,
            NetsuiteSubsidiaryInternalId = dto.NetsuiteSubsidiaryInternalId,
            NetsuiteLocationInternalId = dto.NetsuiteLocationInternalId,
            LocationName = dto.LocationName,
            LocationUsedBin = dto.LocationUsedBin,
            IsLocationUsedBin = IsNetSuiteTrue(dto.LocationUsedBin),
            LineSequenceNumber = dto.LineSequenceNumber,
            TransactionLineType = dto.TransactionLineType,
            NetsuiteVendorInternalId = dto.NetsuiteVendorInternalId,
            VendorName = dto.VendorName,
            VendorBinAssignmentId = dto.VendorBinAssignmentId,
            NetsuiteMaterialInternalId = dto.NetsuiteMaterialInternalId,
            MaterialCode = dto.MaterialCode,
            MaterialName = dto.MaterialName,
            MaterialWeight = dto.MaterialWeight,
            NetsuiteMaterialPrefferedBinId = dto.NetsuiteMaterialPrefferedBinId,
            NetsuiteMaterialVendorAssignedBin = dto.NetsuiteMaterialVendorAssignedBin,
            LineQuantity = dto.LineQuantity,
            LineQuantityReceived = dto.LineQuantityReceived,
            NetsuiteUoMInternalId = dto.NetsuiteUoMInternalId,
            UoMName = dto.UoMName,
            UoMRate = dto.UoMRate,
            NetsuiteOrderDocumentDate = dto.NetsuiteOrderDocumentDate,
            NetsuiteOrderCreatedDate = dto.NetsuiteOrderCreatedDate,
            NSLineQuantity = quantityPlanned,
            NSLineQuantityReceived = quantityReceived,
            NSLineQuantityPacked = quantityOpen,
            ScanCount = scannedQuantity > 0 ? 1 : 0,
            ScannedQuantity = scannedQuantity,
            ScannedWeight = 0,
            TotalWeight = scannedQuantity * dto.MaterialWeight,
            IsBad = isBad,
            AlreadyFulfilled = scannedQuantity == quantityOpen
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
