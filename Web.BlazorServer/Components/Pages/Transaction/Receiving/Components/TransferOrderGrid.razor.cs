using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer.Components;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

public partial class TransferOrderGrid
{
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public string? Source { get; set; } = "purchaseorder";

    AppDataGrid<TransferOrderDataGridVM> TransferOrderDataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; }

    string ActionGetPurchaseOrders { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPurchaseOrders);

    TransferOrderStatusVM? StatusFilter = null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(TransferOrderDataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await TransferOrderDataGrid.DataGrid.ReloadSettings();
        await TransferOrderDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<TransferOrderDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetPurchaseOrders, true);

            if (intent.Sorts.Count == 0)
            {
                intent.Sorts.Add(new()
                {
                    Property = "Date",
                    Direction = SortDirectionEnum.Descending
                });
            }

            return await ReceivingHandler.GetTransferOrderDataGridAsync(intent);

            throw new Exception("Invalid source for receiving grid");
        }, AppActionOptionPresets.Loading(ActionGetPurchaseOrders));

        AppBusyService.SetBusy(ActionGetPurchaseOrders, false);
        return DataGridResultVM<TransferOrderDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    Task<(IEnumerable<TransferOrderStatusVM>, int)> GetTransferOrderStatuses(DataGridIntent intent)
    {
        return ReceivingHandler.GetTransferOrderStatuses(intent);
    }

    async Task ApplyStatusFilter(RadzenDataGridColumn<TransferOrderDataGridVM> column)
    {

        column.ClearFilters();
        if (StatusFilter is null)
        {
            await TransferOrderDataGrid.DataGrid.Reload();
            return;
        }

        column.SetFilterOperator(FilterOperator.Equals);
        column.SetFilterValue(StatusFilter.Name);
        column.SetLogicalFilterOperator(LogicalFilterOperator.And);

        await TransferOrderDataGrid.DataGrid.Reload();
    }

    void ViewTransferOrder(TransferOrderDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/receiving/transfer-order/view?ref={purchaseOrder.ReferenceNumber}", true);
}