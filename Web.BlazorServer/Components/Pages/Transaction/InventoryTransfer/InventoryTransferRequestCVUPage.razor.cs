using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Sprache;
using System.Text.RegularExpressions;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer;

public partial class InventoryTransferRequestCVUPage
{

    #region Parameters
    [SupplyParameterFromQuery, Parameter] public int? Ref { get; set; } = null;
    [Parameter] public bool ModalMode { get; set; } = false;
    [Parameter] public PageActionTypeEnum? ModalAction { get; set; } = null;
    #endregion

    #region Injects
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IInventoryTransferHandler InventoryTransferHandler { get; set; } = default!;
    [Inject] IWarehouseMasterDataHandler WarehouseHandler { get; set; } = default!;
    [Inject] ITransferTypeHandler TransferTypeHandler { get; set; } = default!;
    [Inject] ISchoolYearHandler SchoolYearHandler { get; set; } = default!;

    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }

    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionGetInventoryTransferRequest) ||
        AppBusyService.IsBusy(ActionGetWarehouses);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetInventoryTransferRequest);
    bool CreatingNew => Creating && Ref is null;

    readonly string ActionGetInventoryTransferRequest = EnumHelper.GetEnumDescription(AppActions.ViewInventoryTransferRequest);
    readonly string ActionGetWarehouses = EnumHelper.GetEnumDescription(AppActions.GetWarehouses);
    readonly string ActionGetTransferTypes = EnumHelper.GetEnumDescription(AppActions.GetTransferTypes);
    readonly string ActionGetSchoolYears = EnumHelper.GetEnumDescription(AppActions.GetSchoolYears);
    readonly string ActionCreateInventoryTransferRequest = EnumHelper.GetEnumDescription(AppActions.CreateInventoryTransferRequest);

    public IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;

    const string EDIT_EXIT_PROMPT = "Exit without saving changes?";
    const string ITR_NOTFOUND_WARNING = "Inventory Transfer Request Not Found";
    const string VIEW_LIST_URI = "/transactions/inventory/inventory-transfer";
    const string VIEW_ITR_URI = VIEW_LIST_URI + "/inventory-transfer-request/view?ref={0}";
    const string CREATE_ITR_URI = VIEW_LIST_URI + "/inventory-transfer-request/create?ref={0}";
    const string VIEW_LIST_ITR_TAB_URI = VIEW_LIST_URI + "/?T=request";
    const string VIEW_PENDING_ITR_URI = VIEW_LIST_URI + "/pending-inventory-transfer-request/view?ref={0}";

    const string NO_ITEMS_ALERT = "No Items Selected";
    const string NO_ALLOTED_ITEMS_ALERT = "No Items Alloted";
    const string REMOVE_ZERO_ALLOTED_PROMPT = "Some Items have 0 alloted quantities.\nThese items will be removed.";

    List<WarehouseVM> Warehouses { get; set; } = [];
    List<TransferTypeVM> TransferTypes { get; set; } = [];
    List<SchoolYearVM> SchoolYears { get; set; } = [];
    int WarehousesCount { get; set; }
    int SchoolYearsCount { get; set; }

    AppTable<InventoryTransferRequestLineVM> InventoryTransferRequestTable { get; set; } = default!;
    DataGridSettings InventoryTransferRequestTableSettings { get; set; } = new();
    #endregion

    #region Overrides
    protected override void OnParametersSet()
    {
        if (!ModalMode)
            PageAction = PageActionHelper.GetPageActionType(NavManager.Uri);
        else if (ModalMode && ModalAction != null)
            PageAction = (PageActionTypeEnum)ModalAction;
        if (Creating)
        {
            FormData.PreparedBy = AuthenticationService.GetUserName();
        }
        InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionGetInventoryTransferRequest, true);
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

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override async Task HandleSubmit()
    {

        if (FormData.Lines.Count() <= 0)
        {
            ToastService.Warning(NO_ITEMS_ALERT);
            return;
        }

        if (FormData.Lines.All(x => x.AllotedQuantity <= 0))
        {
            ToastService.Warning(NO_ALLOTED_ITEMS_ALERT);
            return;
        }

        if (FormData.Lines.Any(x => x.AllotedQuantity <= 0))
        {
            if (!await AlertService.PromptAsync(REMOVE_ZERO_ALLOTED_PROMPT))
                return;
            FormData.Lines = [.. FormData.Lines.ToList().Where(x => x.AllotedQuantity > 0)];
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateInventoryTransferRequest, true);

            int response = await InventoryTransferHandler.PostInventoryTransferRequestAsync(FormData);

            return response;
        }, AppActionOptionPresets.Confirmed(ActionCreateInventoryTransferRequest));

        action.OnSuccess(async (args) =>
        {
            NavManager.NavigateTo(string.Format(VIEW_PENDING_ITR_URI, action.Result), true);
        });
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }
    #endregion Overrides

    #region Custom Functions
    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetInventoryTransferRequest))
        {
            AppBusyService.SetBusy(ActionGetInventoryTransferRequest, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await Task.WhenAll(
            LoadTransferTypes(),
            LoadWarehouses(new()),
            LoadSchoolYears(new()),
            GetInventoryTransferRequest()
        );

        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
    }

    async Task GetInventoryTransferRequest()
    {
        if (Ref is null)
        {
            AppBusyService.SetBusy(ActionGetInventoryTransferRequest, false);
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {

            var result = await InventoryTransferHandler.GetInventoryTransferRequestAsync((int)Ref);

            AppBusyService.SetBusy(ActionGetInventoryTransferRequest, false);
            return result;

        }, AppActionOptionPresets.Loading(ActionGetInventoryTransferRequest));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error(ITR_NOTFOUND_WARNING);
            else
            {
                action.Result.Adapt(FormData);

                if (Creating)
                    FormData.PreparedBy = AuthenticationService.GetUserName();
            }
        });

        action.OnFailure(async (args) =>
        {
            NavManager.NavigateTo(VIEW_LIST_URI, true);
        });
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

    async Task LoadTransferTypes()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            await Task.Yield();

            AppBusyService.SetBusy(ActionGetTransferTypes, true);

            IEnumerable<TransferTypeVM> data = await TransferTypeHandler.GetTransferTypesAsync();
            TransferTypes = [.. data];

            AppBusyService.SetBusy(ActionGetTransferTypes, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetTransferTypes));
    }

    async Task LoadGridSettings()
    {
        try
        {
            await GridSettingsService.SetGridSettings(
                InventoryTransferRequestTable.DataGrid,
                settings => InventoryTransferRequestTableSettings = settings ?? new()
            );

            GridSettingsLoaded = true;

            await InventoryTransferRequestTable.DataGrid.ReloadSettings();
            await InventoryTransferRequestTable.DataGrid.Reload();
        }
        catch (Exception ex)
        {
            ToastService.Warning(ex.Message, "An error happened while loading the settings");
        }
    }

    async Task Return()
    {
        if (Creating)
        {
            if (!await AlertService.PromptAsync(header: EDIT_EXIT_PROMPT))
                return;

            if (CreatingNew)
                NavManager.NavigateTo(VIEW_LIST_URI);
            else
                NavManager.NavigateTo(string.Format(VIEW_ITR_URI, Ref), true);
        }
        else
            NavManager.NavigateTo(VIEW_LIST_URI);
    }

    void OnEditButtonPressed()
    {
        FormData.PreparedBy = AuthenticationService.GetUserName();
        NavManager.NavigateTo(string.Format(CREATE_ITR_URI, Ref), true);
    }

    async Task RemoveLine(InventoryTransferRequestLineVM item)
    {
        FormData.Lines = [.. FormData.Lines.Except([item])];
    }

    void OnSourceWarehouseChanged()
    {
        if (CreatingNew) FormData.Lines.Clear();
    }
    #endregion Custom Functions
}

