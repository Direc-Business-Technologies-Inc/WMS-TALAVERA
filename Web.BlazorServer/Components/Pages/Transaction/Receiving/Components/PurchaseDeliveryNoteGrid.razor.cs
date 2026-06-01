using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

public partial class PurchaseDeliveryNoteGrid
{
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<PurchaseDeliveryNoteDataGridVM> PurchaseDeliveryNoteDataGrid { get; set; }
    DataGridSettings PurchaseDeliveryNoteDataGridSettings { get; set; }

    string ActionGetPurchaseDeliveryNotes { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPurchaseDeliveryNotes);

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
        await GridSettingsService.SetGridSettings(PurchaseDeliveryNoteDataGrid.DataGrid, settings => PurchaseDeliveryNoteDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await PurchaseDeliveryNoteDataGrid.DataGrid.ReloadSettings();
        await PurchaseDeliveryNoteDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<PurchaseDeliveryNoteDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetPurchaseDeliveryNotes, true);

            var response = await ReceivingHandler.GetPurchaseDeliveryNoteDataGridAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetPurchaseDeliveryNotes));

        AppBusyService.SetBusy(ActionGetPurchaseDeliveryNotes, false);
        return DataGridResultVM<PurchaseDeliveryNoteDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewPurchaseDeliveryNote(PurchaseDeliveryNoteDataGridVM purchaseOrder) => NavManager.NavigateTo($"/transactions/purchasing/receiving/goods-receipt-po/view?ref={purchaseOrder.DocEntry}", true);
}
