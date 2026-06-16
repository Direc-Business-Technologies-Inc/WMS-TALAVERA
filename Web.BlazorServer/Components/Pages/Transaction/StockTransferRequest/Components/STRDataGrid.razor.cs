using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Transaction.Receiving;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest.Components;

partial class STRDataGrid
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter][EditorRequired]
    public required DataGetterDelegate DataGetter { get; init; }

    [Parameter]
    public EventCallback OnAddClicked { get; set; }

    AppDataGrid<StockTransferRequestDataGridVM> DataGrid { get; set; }
    DataGridSettings DataGridSettings { get; set; }

    readonly string ActionGetStockTransferRequests = "get things from db";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }


    async Task<DataGridResultVM<StockTransferRequestDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetStockTransferRequests, true);

            if (intent.Sorts.Count == 0)
            {
                intent.Sorts.Add(new()
                {
                    Property = "Date",
                    Direction = SortDirectionEnum.Descending
                });
            }

            return await DataGetter(intent);

            throw new Exception("Invalid source for receiving grid");
        }, AppActionOptionPresets.Loading(ActionGetStockTransferRequests));

        AppBusyService.SetBusy(ActionGetStockTransferRequests, false);
        return DataGridResultVM<StockTransferRequestDataGridVM>.New(action.Result.data ?? [], action.Result.count);
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    void ViewSTR(StockTransferRequestDataGridVM item)
    {
        NavManager.NavigateTo(STRRoutes.View + $"?ref={item.ReferenceNumber}");
    }

    async Task AddButtonPressed()
    {
        if (OnAddClicked.HasDelegate) await OnAddClicked.InvokeAsync();
    }

    public delegate Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> DataGetterDelegate(DataGridIntent intent);
}
