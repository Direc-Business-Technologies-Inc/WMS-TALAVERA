using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Common;
using Shared.Services.Repository;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;
using WebLocationVM = Web.BlazorServer.ViewModels.Others.LocationVM;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting;

public partial class InventoryWorksheetPage
{
    [Inject] IInventoryCountingHandler InventoryCountingHandler { get; set; } = default!;
    [Inject] ILocationHandler LocationHandler { get; set; } = default!;
    [Inject] ICurrentUserService _currentUser { get; set; } = default!;

    readonly string ActionGetItems = EnumHelper.GetEnumDescription(AppActions.GetInventoryWorksheetItems);
    readonly string ActionGetLocations = EnumHelper.GetEnumDescription(AppActions.GetInventoryWorksheetLocations);
    readonly string ActionPost = EnumHelper.GetEnumDescription(AppActions.PostInventoryWorksheet);

    bool IsLoadingData =>
        AppBusyService.IsBusy(ActionGetItems) ||
        AppBusyService.IsBusy(ActionGetLocations);

    InventoryWorksheetCreateVM FormData { get; set; } = new();
    List<LocationVM> Locations { get; set; } = [];
    List<InventoryItemVM> InventoryItems { get; set; } = [];
    bool LocationHasBins { get; set; }

    AppTableWithSettings<InventoryWorksheetCreateLineVM> WorksheetTable { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadDataAsync()
    {
        await LoadLocationsAsync();
        await LoadItemsAsync();
    }

    async Task LoadLocationsAsync()
    {
        if (_currentUser.NsSubsidiaryId <= 0)
        {
            Locations = [];
            ToastService.Warning("Current user has no NetSuite subsidiary assigned.");
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetLocations, true);

            (var data, _) = await LocationHandler.GetLocationsBySubsidiaryAsync(
                new DataGridIntent { Take = 50 },
                _currentUser.NsSubsidiaryId);

            return data.Select(CreateLocation);
        }, AppActionOptionPresets.Loading(ActionGetLocations));

        AppBusyService.SetBusy(ActionGetLocations, false);

        action.OnSuccess(result =>
        {
            Locations = result is null ? [] : [.. result];
            return Task.CompletedTask;
        });
    }

    async Task LoadItemsAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetItems, true);
            return await InventoryCountingHandler.GetInventoryWorksheetItemsAsync();
        }, AppActionOptionPresets.Loading(ActionGetItems));

        AppBusyService.SetBusy(ActionGetItems, false);

        action.OnSuccess(result =>
        {
            InventoryItems = result is null ? [] : [.. result];
            return Task.CompletedTask;
        });
    }

    async Task OnLocationChanged(object value)
    {
        LocationHasBins = false;
        ClearForm();

        if (FormData.Location is null || FormData.Location.NetsuiteLocationInternalId <= 0)
        {
            ClearBins();
            return;
        }

        (var bins, int count) = await LocationHandler.GetLocationBinsAsync(
            FormData.Location.NetsuiteLocationInternalId,
            new DataGridIntent { Take = 1 });

        LocationHasBins = count > 0 || bins.Any();

        if (!LocationHasBins)
            ClearBins();
    }

    void ClearForm()
    {
        FormData.Lines = [];
    }

    void ClearBins()
    {
        foreach (var detail in FormData.Lines.SelectMany(line => line.Details))
        {
            detail.Bin = null;
            detail.NetsuiteBinInternalId = 0;
        }
    }

    async Task AddLine()
    {
        FormData.Lines.Add(new InventoryWorksheetCreateLineVM());
        await ReloadLinesAsync();
    }

    async Task RemoveLine(InventoryWorksheetCreateLineVM line)
    {
        FormData.Lines.Remove(line);
        await ReloadLinesAsync();
    }

    async Task OnItemChanged(InventoryWorksheetCreateLineVM line)
    {
        if (line.SelectedItem is null)
        {
            line.NetsuiteMaterialInternalId = 0;
            line.MaterialCode = string.Empty;
            line.MaterialName = string.Empty;
            line.MaterialWeight = 0;
            line.TotalQuantity = 0;
            line.Details = [];

            await ReloadLinesAsync();
            return;
        }

        line.NetsuiteMaterialInternalId = line.SelectedItem.NetsuiteMaterialInternalId;
        line.MaterialCode = line.SelectedItem.MaterialCode;
        line.MaterialName = line.SelectedItem.MaterialName;
        line.MaterialWeight = line.SelectedItem.MaterialWeight;
        line.Details = [];

        await ReloadLinesAsync();
    }

    async Task OpenDetailsDialog(InventoryWorksheetCreateLineVM line)
    {
        if (FormData.Location is null || FormData.Location.NetsuiteLocationInternalId <= 0)
        {
            ToastService.Warning("Please select a location first.");
            return;
        }

        if (line.NetsuiteMaterialInternalId <= 0)
        {
            ToastService.Warning("Please select an item first.");
            return;
        }

        if (line.TotalQuantity <= 0)
        {
            ToastService.Warning("Please enter total quantity first.");
            return;
        }

        var details = await DialogService.OpenAsync<InventoryWorksheetDetailDialog>(
            "Worksheet Details",
            new Dictionary<string, object>
            {
                { nameof(InventoryWorksheetDetailDialog.Line), line },
                { nameof(InventoryWorksheetDetailDialog.LocationId), FormData.Location.NetsuiteLocationInternalId },
                { nameof(InventoryWorksheetDetailDialog.RequireBin), LocationHasBins }
            },
            options: new DialogOptions
            {
                Width = "850px",
                CloseDialogOnOverlayClick = false
            });

        if (details is List<InventoryWorksheetDetailLineVM> savedDetails)
        {
            line.Details = savedDetails;
            await ReloadLinesAsync();
        }
    }

    async Task PostWorksheetAsync()
    {
        if (!ValidateWorksheet())
            return;

        List<InventoryWorksheetDetailLineVM> detailLines = [.. FormData.Lines
            .Where(line => line.TotalQuantity > 0)
            .SelectMany(line => line.Details)];

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionPost, true);
            return await InventoryCountingHandler.PostInventoryWorksheetAsync(
                detailLines,
                FormData.Location!.NetsuiteLocationInternalId,
                _currentUser.NsSubsidiaryId);
        }, AppActionOptionPresets.Confirmed(ActionPost));

        AppBusyService.SetBusy(ActionPost, false);

        action.OnSuccess(result =>
        {
            if (!result)
            {
                ToastService.Error("Failed to post inventory worksheet.");
                return Task.CompletedTask;
            }

            ToastService.Success("Inventory worksheet posted successfully.");
            NavManager.NavigateTo("/transactions/inventory/inventory-worksheet", true);
            return Task.CompletedTask;
        });
    }

    bool ValidateWorksheet()
    {
        if (FormData.Location is null || FormData.Location.NetsuiteLocationInternalId <= 0)
        {
            ToastService.Warning("Please select a location.");
            return false;
        }

        List<InventoryWorksheetCreateLineVM> countedLines = [.. FormData.Lines.Where(line => line.TotalQuantity > 0)];

        if (countedLines.Count == 0)
        {
            ToastService.Warning("Please enter total quantity for at least one item.");
            return false;
        }

        if (FormData.Lines.Any(line => line.TotalQuantity < 0))
        {
            ToastService.Warning("Total quantity cannot be negative.");
            return false;
        }

        foreach (var line in countedLines)
        {
            if (line.NetsuiteMaterialInternalId <= 0)
            {
                ToastService.Warning("Please select an item for every counted line.");
                return false;
            }

            if (line.Details.Count == 0)
            {
                ToastService.Warning($"Please add details for {line.MaterialCode}.");
                return false;
            }

            if (line.AllocatedQuantity != line.TotalQuantity)
            {
                ToastService.Warning($"Allocated quantity must equal total quantity for {line.MaterialCode}.");
                return false;
            }

            if (line.Details.Any(detail => detail.Quantity <= 0))
            {
                ToastService.Warning($"Detail quantity must be greater than zero for {line.MaterialCode}.");
                return false;
            }

            if (LocationHasBins && line.Details.Any(detail => detail.NetsuiteBinInternalId <= 0))
            {
                ToastService.Warning($"Bin is required for {line.MaterialCode}.");
                return false;
            }
        }

        return true;
    }

    void OnTotalQuantityChanged(InventoryWorksheetCreateLineVM line)
    {
        if (line.TotalQuantity < line.AllocatedQuantity)
            line.Details = [];
    }

    ButtonStyle DefaultStyle(InventoryWorksheetCreateLineVM line)
    {
        return line.TotalQuantity <= 0
            ? ButtonStyle.Primary 
            : (line.AllocatedQuantity == line.TotalQuantity 
            ? ButtonStyle.Success
            : ButtonStyle.Warning);
    }

    async Task ReloadLinesAsync()
    {
        if (WorksheetTable?.DataGrid is not null)
            await WorksheetTable.DataGrid.Reload();

        await InvokeAsync(StateHasChanged);
    }

    void GoBack() =>
        NavManager.NavigateTo("/transactions/inventory/inventory-counting", true);

    static LocationVM CreateLocation(WebLocationVM location) =>
        new()
        {
            NetsuiteLocationInternalId = location.Id,
            LocationName = location.Name
        };
}
