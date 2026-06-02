using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving;

public partial class PurchaseOrderCVUPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter]
    public int Ref { get; set; }

    [Parameter]
    public bool ModalMode { get; set; } = false;

    [Parameter]
    public PageActionTypeEnum? ModalAction { get; set; } = null;

    #endregion Parameters

    #region Injects
    [Inject] IReceivingHandler ReceivingHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] ISchoolYearHandler SchoolYearHandler { get; set; } = default!;
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }

    bool PasswordVisibility { get; set; } = false;
    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionGetPurchaseOrder) || AppBusyService.IsBusy(ActionCreateGoodsReceiptPO);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetPurchaseOrder);

    readonly string ActionGetPurchaseOrder = EnumHelper.GetEnumDescription(AppActions.ViewPurchaseOrder);
    readonly string ActionCreateGoodsReceiptPO = EnumHelper.GetEnumDescription(AppActions.CreateGoodsReceiptPO);
    readonly string ActionGetSchoolYears = EnumHelper.GetEnumDescription(AppActions.GetSchoolYears);
    readonly string ActionGetPurchaseTypes = EnumHelper.GetEnumDescription(AppActions.GetPurchaseType);

    int SchoolYearsCount { get; set; } = 0;
    #endregion Primitives

    #region Data Structures
    TimeOnly? Time { get; set; }

    AppTable<PurchaseOrderLineVM> PurchaseOrderTable { get; set; } = default!;
    DataGridSettings PurchaseOrderTableSettings { get; set; } = new();

    List<PurchaseTypeVM> PurchaseTypes { get; set; } = [];
    List<SchoolYearVM> SchoolYears { get; set; } = [];
    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Purchase Order",
        Position = 0,
        Icon = "receipt_long",
    }];

    public IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;
    #endregion Data Structures

    #region Overrides
    protected override void OnParametersSet()
    {
        if (!ModalMode)
            PageAction = PageActionHelper.GetPageActionType(NavManager.Uri);
        else if (ModalMode && ModalAction != null)
            PageAction = (PageActionTypeEnum)ModalAction;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionGetPurchaseOrder, true);
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

    protected override async Task HandleSubmit()
    {
        throw new NotImplementedException();
    }

    #endregion Overrides

    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetPurchaseOrder))
        {
            AppBusyService.SetBusy(ActionGetPurchaseOrder, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await GetPurchaseOrder();

        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
    }

    async Task GetPurchaseOrder()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {

            var result = await ReceivingHandler.GetPurchaseOrderAsync(Ref);

            AppBusyService.SetBusy(ActionGetPurchaseOrder, false);
            return result;

        }, AppActionOptionPresets.Loading(ActionGetPurchaseOrder));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Purchase Order not found");
            else
            {
                action.Result.Adapt(FormData);
            }
        });
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(PurchaseOrderTable.DataGrid, settings => PurchaseOrderTableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await PurchaseOrderTable.DataGrid.ReloadSettings();
        await PurchaseOrderTable.DataGrid.Reload();
    }

    async Task Return()
    {
        if (UnsavedChangesService.HasChanges)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Goods Receipt PO Creation"))
                return;

        NavManager.NavigateTo($"/transactions/purchasing/receiving?t=po", true);
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }
}