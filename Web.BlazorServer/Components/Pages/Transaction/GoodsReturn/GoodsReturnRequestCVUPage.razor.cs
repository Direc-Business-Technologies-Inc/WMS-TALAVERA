using Domain.Entities.Enums.Transaction.GoodsReturn;
using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReturn;

public partial class GoodsReturnRequestCVUPage
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
    [Inject] IGoodsReturnHandler GoodsReturnHandler { get; set; } = default!;
    [Inject] IBusinessPartnerHandler BpHandler { get; set; } = default!;
    [Inject] IWarehouseMasterDataHandler WarehouseHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] ISchoolYearHandler SchoolYearHandler { get; set; } = default!;
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }

    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionGetGoodsReturn) || AppBusyService.IsBusy(ActionCreateGoodsReturn);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetGoodsReturn);

    readonly string ActionGetGoodsReturn = EnumHelper.GetEnumDescription(AppActions.ViewGoodsReturn);
    readonly string ActionGetVendors = EnumHelper.GetEnumDescription(AppActions.GetVendors);
    readonly string ActionGetWarehouses = EnumHelper.GetEnumDescription(AppActions.GetWarehouses);
    readonly string ActionCreateGoodsReturn = EnumHelper.GetEnumDescription(AppActions.CreateGoodsReturn);
    readonly string ActionGetReturnTypes = EnumHelper.GetEnumDescription(AppActions.GetReturnTypes);
    readonly string ActionGetSchoolYears = EnumHelper.GetEnumDescription(AppActions.GetSchoolYears);

    int BusinessPartnersCount { get; set; } = 0;
    int WarehousesCount { get; set; } = 0;
    int SchoolYearsCount { get; set; } = 0;

    #endregion Primitives

    #region Data Structures
    AppTable<GRRLineVM> GoodsReturnTable { get; set; } = default!;
    DataGridSettings GoodsReturnTableSettings { get; set; } = new();
    List<BusinessPartnerVM> BusinessPartners { get; set; } = [];
    List<WarehouseVM> Warehouses { get; set; } = [];
    List<SchoolYearVM> SchoolYears { get; set; } = [];
    List<ReturnTypeVM> ReturnTypes { get; set; } = [];

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
        AppBusyService.SetBusy(ActionGetGoodsReturn, true);
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

    protected override async Task CancelEditing()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Goods Return Creation"))
                return;

        NavManager.NavigateTo($"/transactions/purchasing/goods-return/request/view?Ref={FormData.SapReference.DocEntry}", true);
    }

    protected override async Task HandleSubmit()
    {
        if (FormData.DocumentLines.Count <= 0)
        {
            ToastService.Warning("Please select Items to Return");
            return;
        }

        if (FormData.DocumentLines.All(x => x.Quantity <= 0))
        {
            ToastService.Warning("Please provide Quantities to the selected Items");
            return;
        }

        if (FormData.DocumentLines.Any(x => x.Quantity <= 0))
        {
            if (!await AlertService.PromptAsync("Some Items in the Goods Return has no Quantity. These Items will be removed in the transaction. Are you sure wou want to proceed?"))
                return;
            FormData.DocumentLines.RemoveAll(x => x.Quantity <= 0);
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateGoodsReturn, true);

            bool response = await GoodsReturnHandler.PostGoodsReturnAsync(FormData, GoodsReturnPostingSource.GRR);

            return response;
        }, AppActionOptionPresets.Confirmed(ActionCreateGoodsReturn));

        action.OnSuccess(async (args) =>
        {
            NavManager.NavigateTo("/transactions/purchasing/goods-return?t=grr");
        });
    }

    protected override async Task InitializeEditing()
    {
        NavManager.NavigateTo($"/transactions/purchasing/goods-return/request/create?ref={FormData.SapReference.DocEntry}", true);
    }

    #endregion Overrides

    #region Custom Functions

    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetGoodsReturn))
        {
            AppBusyService.SetBusy(ActionGetGoodsReturn, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await Task.WhenAll(
            GetGoodsReturn(),
            LoadVendors(new()),
            LoadReturnTypes(),
            LoadSchoolYears(new()),
            LoadWarehouses(new()));

        FormData.PreparedBy = AuthenticationService.GetUserName();

        AppBusyService.SetBusy(ActionGetGoodsReturn, false);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
    }

    async Task GetGoodsReturn()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            var result = await GoodsReturnHandler.GetGoodsReturnRequestAsync(Ref);

            AppBusyService.SetBusy(ActionGetGoodsReturn, false);
            return result;

        }, AppActionOptionPresets.Loading(ActionGetGoodsReturn));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Goods Return not found");
            else
            {
                action.Result.Adapt(FormData);

                FormData.PreparedBy = AuthenticationService.GetUserName();
            }
        });
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(GoodsReturnTable.DataGrid, settings => GoodsReturnTableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await GoodsReturnTable.DataGrid.ReloadSettings();
        await GoodsReturnTable.DataGrid.Reload();
    }

    async Task Return()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Goods Return Creation"))
                return;

        NavManager.NavigateTo($"/transactions/purchasing/goods-return?t=grr", true);
    }

    async Task LoadVendors(LoadDataArgs args)
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetVendors, true);

            DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(BusinessPartnerVM.CardName),
                    Value = args.Filter,
                    ComparisonOperator = ComparisonOperatorEnum.Contains
                });

            (IEnumerable<BusinessPartnerVM> Data, int Count) = await BpHandler.GetVendorsAsync(DatagridAdapter.QueryIntent);

            BusinessPartners = [.. Data];
            BusinessPartnersCount = Count;

            AppBusyService.SetBusy(ActionGetVendors, false);
        }, AppActionOptionPresets.Loading(ActionGetVendors));
    }

    async Task LoadWarehouses(LoadDataArgs args)
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            await Task.Yield();

            AppBusyService.SetBusy(ActionGetWarehouses, true);

            DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(WarehouseVM.WhsName),
                    Value = args.Filter,
                    ComparisonOperator = ComparisonOperatorEnum.Contains
                });

            (IEnumerable<WarehouseVM> Data, int Count) = await WarehouseHandler.GetWarehousesAsync(DatagridAdapter.QueryIntent);

            Warehouses = [.. Data];
            WarehousesCount = Count;

            AppBusyService.SetBusy(ActionGetWarehouses, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetWarehouses));
    }

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
            AppBusyService.SetBusy(ActionGetReturnTypes, true);


            ReturnTypes = [.. await GoodsReturnHandler.GetReturnTypesAsync()];

            AppBusyService.SetBusy(ActionGetReturnTypes, false);
        }, AppActionOptionPresets.Loading(ActionGetReturnTypes));
    }

    #endregion Custom Functions
}
