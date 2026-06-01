using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Delivery;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Delivery;
using Web.BlazorServer.ViewModels.Transaction.SalesReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SalesReturn.Components;

public partial class SalesReturnDeliverySelection
{
    [Parameter] public SalesReturnVM Document { get; set; } = new();
    [Parameter] public EventCallback<SalesReturnVM> DocumentChanged { get; set; } = new();

    [Inject] IDeliveryHandler DeliveryHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<DeliveryDataGridVM> DeliveryDataGrid { get; set; } = default!;
    DataGridSettings DeliveryDataGridSettings { get; set; } = new();

    string ActionGetDeliveries { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllDeliveries);

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
        await GridSettingsService.SetGridSettings(DeliveryDataGrid.DataGrid, settings => DeliveryDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DeliveryDataGrid.DataGrid.ReloadSettings();
        await DeliveryDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<DeliveryDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetDeliveries, true);
            var response = await DeliveryHandler.GetDeliveryDataGridAsync(intent);
            return response;
        }, AppActionOptionPresets.Loading(ActionGetDeliveries));

        AppBusyService.SetBusy(ActionGetDeliveries, false);
        return DataGridResultVM<DeliveryDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    async Task SelectSource(DeliveryDataGridVM delivery)
    {
        if (!await AlertService.PromptAsync())
            return;

        DeliveryVM? copyFrom = await DeliveryHandler.GetDeliveryAsync(delivery.DocEntry) ?? new();

        if (copyFrom.SapReference.DocEntry <= 0)
        {
            ToastService.Warning("Failed to get the source Delivery. Please try again.");
            return;
        }

        Document.DeliveryDocEntry = copyFrom.SapReference.DocEntry ?? 0;
        Document.DeliveryDocNum = copyFrom.SapReference.DocNum ?? 0;
        Document.SchoolYear = copyFrom.SchoolYear;
        Document.DRNo = copyFrom.DRNo;
        Document.BusinessPartner = copyFrom.BusinessPartner;

        Document.DocumentLines = [.. copyFrom.DocumentLines.Select((dl, idx) => new SalesReturnLineVM
        {
            LineNum = idx + 1,
            BaseEntry = copyFrom.SapReference.DocEntry ?? 0,
            BaseDocNum = copyFrom.SapReference.DocNum ?? 0,
            BaseLine = dl.LineNum,
            ItemCode = dl.ItemCode,
            ItemName = dl.ItemName,
            UoMCode = dl.UoMCode,
            UoMName = dl.UoMName,
            UoMValue = dl.UoMValue,
            TargetQuantity = dl.Quantity,
            OpenQuantity = dl.Quantity,
            Quantity = dl.Quantity,
            Warehouse = dl.Warehouse ?? new(),
        })];

        await DocumentChanged.InvokeAsync(Document);
        DialogService.Close(true);
    }
}
