using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;

public partial class InventoryWorksheetDetailDialog
{
    [Inject] ILocationHandler LocationHandler { get; set; } = default!;

    [Parameter, EditorRequired] public required InventoryWorksheetCreateLineVM Line { get; set; }
    [Parameter] public int LocationId { get; set; }
    [Parameter] public bool RequireBin { get; set; }

    List<InventoryWorksheetDetailLineVM> Details { get; set; } = [];
    InventoryWorksheetDetailStatus[] StatusOptions { get; } = Enum.GetValues<InventoryWorksheetDetailStatus>();

    string ActionGetLocationBins => $"Get Bins For Location {LocationId}";
    decimal CurrentCount => Details.Sum(detail => detail.Quantity);
    decimal RemainingQuantity => Line.TotalQuantity - CurrentCount;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Details = [.. Line.Details.Select(CloneDetail)];
    }

    async Task<(IEnumerable<LocationBinVM> Data, int Count)> LocationBinProvider(DataGridIntent intent) =>
        await LocationHandler.GetLocationBinsAsync(LocationId, intent);

    async Task AddLine()
    {
        Details.Add(new InventoryWorksheetDetailLineVM
        {
            NetsuiteMaterialInternalId = Line.NetsuiteMaterialInternalId,
            MaterialCode = Line.MaterialCode,
            MaterialName = Line.MaterialName,
            MaterialWeight = Line.MaterialWeight,
            Status = InventoryWorksheetDetailStatus.Good
        });

        await InvokeAsync(StateHasChanged);
    }

    async Task RemoveLine(InventoryWorksheetDetailLineVM detail)
    {
        Details.Remove(detail);
        await InvokeAsync(StateHasChanged);
    }

    decimal GetLineMax(InventoryWorksheetDetailLineVM detail) =>
        Line.TotalQuantity - Details.Where(line => !ReferenceEquals(line, detail)).Sum(line => line.Quantity);

    void Cancel() =>
        DialogService.Close(null);

    void Save()
    {
        if (Line.TotalQuantity <= 0)
        {
            ToastService.Warning("Total quantity must be greater than zero.");
            return;
        }

        if (Details.Count == 0)
        {
            ToastService.Warning("Please add at least one detail line.");
            return;
        }

        if (Details.Any(detail => detail.Quantity <= 0))
        {
            ToastService.Warning("Detail quantity must be greater than zero.");
            return;
        }

        if (CurrentCount > Line.TotalQuantity)
        {
            ToastService.Warning("Detail quantity cannot exceed total quantity.");
            return;
        }

        if (RequireBin && Details.Any(detail => detail.Bin is null || detail.Bin.Id <= 0))
        {
            ToastService.Warning("Bin is required for the selected location.");
            return;
        }

        List<InventoryWorksheetDetailLineVM> savedDetails = [.. Details.Select(ToSavedDetail)];
        DialogService.Close(savedDetails);
    }

    InventoryWorksheetDetailLineVM ToSavedDetail(InventoryWorksheetDetailLineVM detail)
    {
        InventoryWorksheetDetailLineVM saved = CloneDetail(detail);
        saved.NetsuiteMaterialInternalId = Line.NetsuiteMaterialInternalId;
        saved.MaterialCode = Line.MaterialCode;
        saved.MaterialName = Line.MaterialName;
        saved.MaterialWeight = Line.MaterialWeight;
        saved.NetsuiteBinInternalId = RequireBin ? saved.Bin?.Id ?? 0 : 0;
        saved.GoodScannedQuantity = saved.Status == InventoryWorksheetDetailStatus.Good ? saved.Quantity : 0;
        saved.BadScannedQuantity = saved.Status == InventoryWorksheetDetailStatus.Bad ? saved.Quantity : 0;
        saved.ScanCount = 0;

        return saved;
    }

    static InventoryWorksheetDetailLineVM CloneDetail(InventoryWorksheetDetailLineVM detail) =>
        new()
        {
            NetsuiteInventoryDetailInternalId = detail.NetsuiteInventoryDetailInternalId,
            NetsuiteMaterialInternalId = detail.NetsuiteMaterialInternalId,
            MaterialCode = detail.MaterialCode,
            MaterialName = detail.MaterialName,
            MaterialWeight = detail.MaterialWeight,
            GoodScannedQuantity = detail.GoodScannedQuantity,
            BadScannedQuantity = detail.BadScannedQuantity,
            NetsuiteBinInternalId = detail.NetsuiteBinInternalId,
            ScanCount = detail.ScanCount,
            Bin = detail.Bin,
            Status = detail.Status,
            Quantity = detail.Quantity
        };
}
