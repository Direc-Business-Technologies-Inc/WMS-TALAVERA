using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer.Components;

partial class DataGrid
{
    [Parameter, EditorRequired] public string ActionName { get; set; }
    [Parameter] public string ViewItemURI { get; set; } = string.Empty;
    [Parameter] public bool IsDraft { get; set; } = false;
    [Parameter] public string? CreateItemURI { get; set; } = null;
    [Parameter] public Func<DataGridIntent, Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)>> DataGetter { get; set; }


    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    AppDataGrid<InventoryTransferRequestDataGridVM> InventoryTransferRequestDataGrid { get; set; }
    DataGridSettings InventoryTransferRequestDataGridSettings { get; set; }


    async Task LoadGridSettings()
    {
        try
        {
            await GridSettingsService.SetGridSettings(
                InventoryTransferRequestDataGrid.DataGrid,
                settings => InventoryTransferRequestDataGridSettings = settings ?? new()
                );
            GridSettingsLoaded = true;

            await InventoryTransferRequestDataGrid.DataGrid.ReloadSettings();
            await InventoryTransferRequestDataGrid.DataGrid.Reload();
        }
        catch (Exception ex)
        {
            ToastService.Warning(ex.Message, "An error happened while loading the settings");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task<DataGridResultVM<InventoryTransferRequestDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionName, true);

            var response = await DataGetter(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionName));

        AppBusyService.SetBusy(ActionName, false);
        return DataGridResultVM<InventoryTransferRequestDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void CreateItem()
    {
        if (CreateItemURI is not null)
            NavManager.NavigateTo(string.Format(CreateItemURI), true);
    }
    void ViewInventoryTransferRequest(InventoryTransferRequestDataGridVM itrVM) => NavManager.NavigateTo(string.Format(ViewItemURI, itrVM.DocEntry), true);
}
