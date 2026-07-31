using Application.DataTransferObjects.Others.SAP;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS.Request;
using Application.UseCases.Commands.Transaction.InventoryCounting;
using Application.UseCases.Commands.Transaction.InventoryCounting.NS;
using Application.UseCases.Queries.Others.NS;
using Application.UseCases.Queries.Transaction.InventoryCounting;
using Application.UseCases.Queries.Transaction.InventoryCounting.NS;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;
using SharedInventoryItemVM = Shared.Libraries.ViewModel.Common.InventoryItemVM;
using SharedInventoryCountingLineVM = Shared.Libraries.ViewModel.InventoryCounting.InventoryCountingLineVM;
using SharedInventoryCountingVM = Shared.Libraries.ViewModel.InventoryCounting.InventoryCountingVM;
using SharedItemBarcodesPerUoMVM = Shared.Libraries.ViewModel.ItemBarcodesPerUoMVM;
using SharedLocationVM = Shared.Libraries.ViewModel.LocationVM;
using GetNSLocationsQry = Application.UseCases.Queries.Others.NS.GetLocationsQry;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryCounting;

public class InventoryCountingHandler(ISender Sender) : IInventoryCountingHandler
{
    public async Task<(IEnumerable<SharedInventoryCountingVM> Data, int Count)> GetStartedInventoryCountingAsync(DataGridIntent intent, int subsidiaryId)
    {
        RequestPerUserDTO subsidiaryDTO = new()
        {
            NetsuiteUserSubsidiaryInternalId = subsidiaryId
        };

        IEnumerable<OrdersDTO> response = await Sender.Send(new GetStartedInventoryCountingQry(subsidiaryDTO));
        List<SharedInventoryCountingVM> data = response.Adapt<List<SharedInventoryCountingVM>>();

        foreach (var sort in intent.Sorts)
            data = ApplySort(data, sort);

        int count = data.Count;
        IEnumerable<SharedInventoryCountingVM> pagedData = data;

        if (intent.Take > 0)
            pagedData = pagedData.Skip(intent.Skip).Take(intent.Take);

        return (pagedData, count);
    }

    public async Task<IEnumerable<SharedInventoryCountingLineVM>> GetStartedInventoryCountingLinesAsync(string orderNumber)
    {
        GetStartedInventoryCountingLineQry qry = new(new InventoryCountingLineRequestDTO
        {
            OrderNumber = orderNumber
        });

        IEnumerable<InventoryCountingLineDTO> response = await Sender.Send(qry);
        return response.Adapt<IEnumerable<SharedInventoryCountingLineVM>>();
    }

    public async Task<bool> PatchStartedInventoryCountingAsync(IEnumerable<SharedInventoryCountingLineVM> lines)
    {
        List<PatchInventoryCountingDTO> patchLines =
        [
            .. lines
                .Where(line => line.ScannedQuantity > 0)
                .Select(CreatePatchLine)
        ];

        if (patchLines.Count == 0)
            return false;

        PatchInventoryCountingCmd cmd = new(patchLines);
        var result = await Sender.Send(cmd);

        return result.Success && result.Data == true;
    }

    public async Task<IEnumerable<SharedInventoryItemVM>> GetInventoryWorksheetItemsAsync()
    {
        IEnumerable<InventoryItemDTO> response = await Sender.Send(new GetInventoryItemsQry());
        return response.Adapt<IEnumerable<SharedInventoryItemVM>>();
    }

    public async Task<IEnumerable<SharedLocationVM>> GetInventoryWorksheetLocationsAsync()
    {
        IEnumerable<LocationDTO> response = await Sender.Send(new GetNSLocationsQry());
        return response.Adapt<IEnumerable<SharedLocationVM>>();
    }

    public async Task<IEnumerable<SharedItemBarcodesPerUoMVM>> GetInventoryWorksheetItemBarcodesAsync(IEnumerable<int> itemIds)
    {
        List<ItemBarcodesRequestDTO> request = [.. itemIds
            .Distinct()
            .Select(itemId => new ItemBarcodesRequestDTO
            {
                NetsuiteMaterialInternalId = itemId
            })];

        if (request.Count == 0)
            return [];

        IEnumerable<ItemBarcodesPerUoMDTO> response = await Sender.Send(new GetItemBarcodesPerUoMQry(request));
        return response.Adapt<IEnumerable<SharedItemBarcodesPerUoMVM>>();
    }

    public async Task<bool> PostInventoryWorksheetAsync(IEnumerable<InventoryWorksheetDetailLineVM> lines, int locationId, int subsidiaryId)
    {
        List<InventoryWorksheetLineDTO> worksheetLines = [.. lines
            .Where(line => line.Quantity > 0)
            .Select(CreateWorksheetLine)];

        if (worksheetLines.Count == 0)
            return false;

        PostInventoryWorksheetCmd cmd = new(worksheetLines, locationId, subsidiaryId);
        var result = await Sender.Send(cmd);

        return result.Success && result.Data == true;
    }

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

    static List<SharedInventoryCountingVM> ApplySort(List<SharedInventoryCountingVM> data, AppSortDescriptor sort)
    {
        var property = typeof(SharedInventoryCountingVM).GetProperty(sort.Property);

        if (property is null)
            return data;

        return sort.Direction == SortDirectionEnum.Descending
            ? [.. data.OrderByDescending(item => property.GetValue(item))]
            : [.. data.OrderBy(item => property.GetValue(item))];
    }

    static PatchInventoryCountingDTO CreatePatchLine(SharedInventoryCountingLineVM line) =>
        new()
        {
            NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
            OrderNumber = line.OrderNumber,
            OrderType = line.OrderType,
            OrderStatus = line.OrderStatus,
            NetsuiteSubsidiaryInternalId = line.NetsuiteSubsidiaryInternalId,
            NetsuiteLocationInternalId = line.NetsuiteLocationInternalId,
            LocationName = line.LocationName,
            LineSequenceNumber = line.LineSequenceNumber,
            TransactionLineType = line.TransactionLineType,
            NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
            MaterialCode = line.MaterialCode,
            MaterialName = line.MaterialName,
            MaterialWeight = line.MaterialWeight,
            LineQuantity = line.LineQuantity,
            LineQuantityReceived = line.LineQuantityReceived,
            LineQuantityPacked = line.LineQuantityPacked,
            LineQuantityShipped = line.LineQuantityShipped,
            NetsuiteUoMInternalId = line.NetsuiteUoMInternalId,
            UoMName = line.UoMName,
            UoMRate = line.UoMRate,
            NetsuiteOrderCreatedDate = line.NetsuiteOrderCreatedDate,
            NetsuiteOrderDocumentDate = line.NetsuiteOrderDocumentDate,
            NetsuiteOrderUpdatedDate = line.NetsuiteOrderUpdatedDate,
            NetsuiteInventoryDetailInternalId = line.NetsuiteInventoryDetailInternalId,
            ScannedQuantity = line.ScannedQuantity,
            IsBad = false
        };

    static InventoryWorksheetLineDTO CreateWorksheetLine(InventoryWorksheetDetailLineVM line) =>
        new()
        {
            NetsuiteInventoryDetailInternalId = line.NetsuiteInventoryDetailInternalId,
            NetsuiteMaterialInternalId = line.NetsuiteMaterialInternalId,
            MaterialCode = line.MaterialCode,
            MaterialName = line.MaterialName,
            MaterialWeight = line.MaterialWeight,
            GoodScannedQuantity = line.Status == InventoryWorksheetDetailStatus.Good ? line.Quantity : 0,
            BadScannedQuantity = line.Status == InventoryWorksheetDetailStatus.Bad ? line.Quantity : 0,
            NetsuiteBinInternalId = line.NetsuiteBinInternalId,
            ScanCount = line.ScanCount
        };
}
