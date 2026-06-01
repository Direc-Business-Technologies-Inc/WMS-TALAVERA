using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Delivery;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.Delivery;

namespace Web.BlazorServer.Components.Pages.Transaction.Delivery;

public partial class SalesOrderCVUPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter]
    public int Ref { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] IDeliveryHandler DeliveryHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; } = PageActionTypeEnum.View;

    bool IsBusy => AppBusyService.IsBusy(ActionGetSalesOrder);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetSalesOrder);

    readonly string ActionGetSalesOrder = EnumHelper.GetEnumDescription(AppActions.ViewSalesOrder);
    #endregion Primitives

    #region Data Structures
    AppTable<SalesOrderLineVM> SalesOrderTable { get; set; } = default!;
    DataGridSettings SalesOrderTableSettings { get; set; } = new();

    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Sales Order",
        Position = 0,
        Icon = "receipt_long",
    }];
    #endregion Data Structures

    #region Overrides
    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionGetSalesOrder, true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        return Task.CompletedTask;
    }
    #endregion Overrides

    #region Custom Functions
    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetSalesOrder))
        {
            AppBusyService.SetBusy(ActionGetSalesOrder, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await GetSalesOrder();

        AppBusyService.SetBusy(ActionGetSalesOrder, false);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
    }

    async Task GetSalesOrder()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            var result = await DeliveryHandler.GetSalesOrderAsync(Ref);
            AppBusyService.SetBusy(ActionGetSalesOrder, false);
            return result;
        }, AppActionOptionPresets.Loading(ActionGetSalesOrder));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Sales Order not found");
            else
                action.Result.Adapt(FormData);
        });
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(SalesOrderTable.DataGrid, settings => SalesOrderTableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await SalesOrderTable.DataGrid.ReloadSettings();
        await SalesOrderTable.DataGrid.Reload();
    }

    void Back() => NavManager.NavigateTo("/transactions/sales/delivery?T=so", true);

    void CreateDelivery() => NavManager.NavigateTo($"/transactions/sales/delivery/create?ref={Ref}");
    #endregion Custom Functions
}
