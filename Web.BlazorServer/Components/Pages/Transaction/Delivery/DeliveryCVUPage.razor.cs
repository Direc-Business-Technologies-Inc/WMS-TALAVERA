using Domain.Providers;
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

public partial class DeliveryCVUPage
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
    PageActionTypeEnum PageAction { get; set; }

    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionGetDelivery) || AppBusyService.IsBusy(ActionCreateDelivery);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetDelivery);

    DateTime ActualDeliveryDate { get; set; } = DateTimeProvider.Now;

    readonly string ActionGetDelivery = EnumHelper.GetEnumDescription(AppActions.ViewDelivery);
    readonly string ActionCreateDelivery = EnumHelper.GetEnumDescription(AppActions.CreateDelivery);
    readonly string ActionGetDeliveryMeans = EnumHelper.GetEnumDescription(AppActions.GetDeliveryMeans);
    #endregion Primitives

    #region Data Structures
    SalesOrderVM SalesOrderData { get; set; } = new();
    List<DeliveryMeansVM> DeliveryMeans { get; set; } = [];

    AppTable<SalesOrderLineVM> SalesOrderLinesTable { get; set; } = default!;
    DataGridSettings SalesOrderLinesTableSettings { get; set; } = new();

    AppTable<DeliveryLineVM> DeliveryLinesTable { get; set; } = default!;
    DataGridSettings DeliveryLinesTableSettings { get; set; } = new();

    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Delivery",
        Position = 0,
        Icon = "local_shipping",
    }];
    #endregion Data Structures

    #region Overrides
    protected override void OnParametersSet()
    {
        PageAction = PageActionHelper.GetPageActionType(NavManager.Uri);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionGetDelivery, true);
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

    protected override async Task HandleSubmit()
    {
        if (Creating && !SalesOrderData.DocumentLines.Any(l => l.Quantity > 0))
        {
            ToastService.Warning("Please enter a delivery quantity for at least one item");
            return;
        }

        if (Creating && SalesOrderData.DocumentLines.Any(l => l.Quantity > l.OpenQty))
        {
            ToastService.Warning("Delivery quantity cannot exceed the open quantity for one or more items");
            return;
        }

        if (Creating)
            FormData.DocumentLines = [.. SalesOrderData.DocumentLines.Adapt<IEnumerable<DeliveryLineVM>>()];

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateDelivery, true);

            bool response = await DeliveryHandler.PostDeliveryAsync(FormData);

            return response;
        }, AppActionOptionPresets.Confirmed(ActionCreateDelivery));

        action.OnSuccess(async (args) =>
        {
            NavManager.NavigateTo("/transactions/sales/delivery?T=dlv", true);
        });
    }
    #endregion Overrides

    #region Custom Functions
    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetDelivery))
        {
            AppBusyService.SetBusy(ActionGetDelivery, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        if (Creating)
            await Task.WhenAll(GetSalesOrder(), LoadDeliveryMeans());
        else
            await Task.WhenAll(GetDelivery(), LoadDeliveryMeans());

        FormData.PreparedBy = AuthenticationService.GetUserName();

        AppBusyService.SetBusy(ActionGetDelivery, false);
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
            AppBusyService.SetBusy(ActionGetDelivery, false);
            return result;
        }, AppActionOptionPresets.Loading(ActionGetDelivery));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Sales Order not found");
            else
            {
                SalesOrderData = action.Result;
                FormData.SapReference.BaseEntry = action.Result.SapReference.DocEntry;
                FormData.BusinessPartner = action.Result.BusinessPartner;
                FormData.ContactPerson = action.Result.ContactPerson;
                FormData.SchoolYear = action.Result.SchoolYear;
                FormData.Designation = action.Result.Designation;
                FormData.DocDate = action.Result.DocDate;
                FormData.PostingDate = DateTime.Today;
                FormData.DeliveryDate = action.Result.DocDueDate;
                FormData.DocumentDate = DateTime.Today;
            }
        });
    }

    async Task GetDelivery()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            var result = await DeliveryHandler.GetDeliveryAsync(Ref);
            AppBusyService.SetBusy(ActionGetDelivery, false);
            return result;
        }, AppActionOptionPresets.Loading(ActionGetDelivery));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Delivery document not found");
            else
                action.Result.Adapt(FormData);
        });
    }

    async Task LoadDeliveryMeans()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetDeliveryMeans, true);

            DeliveryMeans = [.. await DeliveryHandler.GetDeliveryMeansAsync()];

            AppBusyService.SetBusy(ActionGetDeliveryMeans, false);
        }, AppActionOptionPresets.Loading(ActionGetDeliveryMeans));
    }

    async Task LoadGridSettings()
    {
        GridSettingsLoaded = true;

        if (Creating && SalesOrderLinesTable is not null)
        {
            await GridSettingsService.SetGridSettings(SalesOrderLinesTable.DataGrid, settings => SalesOrderLinesTableSettings = settings ?? new());
            await SalesOrderLinesTable.DataGrid.ReloadSettings();
            await SalesOrderLinesTable.DataGrid.Reload();
        }
        else if (Viewing && DeliveryLinesTable is not null)
        {
            await GridSettingsService.SetGridSettings(DeliveryLinesTable.DataGrid, settings => DeliveryLinesTableSettings = settings ?? new());
            await DeliveryLinesTable.DataGrid.ReloadSettings();
            await DeliveryLinesTable.DataGrid.Reload();
        }
    }

    async Task Back()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Delivery Creation"))
                return;

        string tab = Creating ? "so" : "dlv";
        NavManager.NavigateTo($"/transactions/sales/delivery?T={tab}", true);
    }


    void ParseActualDeliveryDate()
    {
        FormData.ActualDelivDate = ActualDeliveryDate.ToString("MM-dd-yy");
    }

    #endregion Custom Functions
}
