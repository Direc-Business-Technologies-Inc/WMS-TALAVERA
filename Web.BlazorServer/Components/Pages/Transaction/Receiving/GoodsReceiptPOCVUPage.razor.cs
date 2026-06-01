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

public partial class GoodsReceiptPOCVUPage
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
    bool IsBusy => AppBusyService.IsBusy(ActionGetPurchaseDeliveryNote);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetPurchaseDeliveryNote);

    readonly string ActionGetPurchaseDeliveryNote = EnumHelper.GetEnumDescription(AppActions.ViewPurchaseDeliveryNote);
    readonly string ActionGetSchoolYears = EnumHelper.GetEnumDescription(AppActions.GetSchoolYears);
    readonly string ActionGetPurchaseTypes = EnumHelper.GetEnumDescription(AppActions.GetPurchaseType);

    int SchoolYearsCount { get; set; } = 0;
    #endregion Primitives

    #region Data Structures
    TimeOnly? Time { get; set; }

    AppTable<PurchaseDeliveryNoteLineVM> PurchaseDeliveryNoteTable { get; set; } = default!;
    DataGridSettings PurchaseDeliveryNoteTableSettings { get; set; } = new();

    List<PurchaseTypeVM> PurchaseTypes { get; set; } = [];
    List<SchoolYearVM> SchoolYears { get; set; } = [];
    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Goods Receipt Purchase Order",
        Position = 0,
        Icon = "assignment",
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
        AppBusyService.SetBusy(ActionGetPurchaseDeliveryNote, true);
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

    protected override async Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    protected override async Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override async Task HandleSubmit()
    {
        throw new NotImplementedException();
    }

    #endregion Overrides

    #region Custom Function

    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetPurchaseDeliveryNote))
        {
            AppBusyService.SetBusy(ActionGetPurchaseDeliveryNote, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await Task.WhenAll(
            LoadReturnTypes(),
            GetPurchaseDeliveryNote());

        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
    }

    async Task GetPurchaseDeliveryNote()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {

            var result = await ReceivingHandler.GetPurchaseDeliveryNoteAsync(Ref);

            AppBusyService.SetBusy(ActionGetPurchaseDeliveryNote, false);
            return result;

        }, AppActionOptionPresets.Loading(ActionGetPurchaseDeliveryNote));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Purchase Order not found");
            else
            {
                action.Result.Adapt(FormData);

                if (Creating)
                    FormData.ReceivedBy = AuthenticationService.GetUserName();
                if (Viewing)
                    ParseTimeFromInt();
            }
        });
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(PurchaseDeliveryNoteTable.DataGrid, settings => PurchaseDeliveryNoteTableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await PurchaseDeliveryNoteTable.DataGrid.ReloadSettings();
        await PurchaseDeliveryNoteTable.DataGrid.Reload();
    }

    async Task Return() => NavManager.NavigateTo($"/transactions/purchasing/receiving?t=grpo", true);

    async Task LoadSchoolYears(LoadDataArgs args)
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            await Task.Yield();

            AppBusyService.SetBusy(ActionGetSchoolYears, true);

            DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(SchoolYearVM.Code),
                    Value = args.Filter,
                    ComparisonOperator = ComparisonOperatorEnum.Contains
                });

            (IEnumerable<SchoolYearVM> Data, int Count) = await SchoolYearHandler.GetSchoolYearsAsync(DatagridAdapter.QueryIntent);

            SchoolYears = [.. Data];
            SchoolYearsCount = Count;

            AppBusyService.SetBusy(ActionGetSchoolYears, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetSchoolYears));
    }

    async Task LoadReturnTypes()
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetPurchaseTypes, true);


            PurchaseTypes = [.. await ReceivingHandler.GetPurchaseTypesAsync()];

            AppBusyService.SetBusy(ActionGetPurchaseTypes, false);
        }, AppActionOptionPresets.Loading(ActionGetPurchaseTypes));
    }

    void ParseIntTime()
    {
        if (Time is null)
            return;

        FormData.Time = Time.Value.Hour * 100 + Time.Value.Minute;
    }

    void ParseTimeFromInt()
    {
        if (FormData.Time is null)
            return;

        int hours = FormData.Time.Value / 100;
        int minutes = FormData.Time.Value % 100;

        Time = new TimeOnly(hours, minutes);
    }
    #endregion Custom Function
}
