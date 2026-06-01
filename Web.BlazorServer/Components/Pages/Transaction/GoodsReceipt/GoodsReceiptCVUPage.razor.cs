using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Others;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReceipt;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using Web.BlazorServer.ViewModels.Transaction.GoodsReceipt;
using Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReceipt;

public partial class GoodsReceiptCVUPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter]
    public int Ref { get; set; }

    [SupplyParameterFromQuery]
    [Parameter]
    public int Draft { get; set; }

    [Parameter]
    public bool ModalMode { get; set; } = false;

    [Parameter]
    public PageActionTypeEnum? ModalAction { get; set; } = null;

    #endregion Parameters

    #region Injects
    [Inject] IGoodsReceiptHandler GoodsReceiptHandler { get; set; } = default!;
    [Inject] IBusinessPartnerHandler BpHandler { get; set; } = default!;
    [Inject] ITransactionTypeHandler TransTypeHandler { get; set; } = default!;
    [Inject] IWarehouseMasterDataHandler WarehouseHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IBusinessPartnerHandler BusinessPartnerHandler { get; set; } = default!;
    [Inject] IItemMasterDataHandler ItemMasterDataHandler { get; set; } = default!;
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }

    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionGetGoodsReceipt) || AppBusyService.IsBusy(ActionCreateGoodsReceipt);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetGoodsReceipt);

    readonly string ActionGetGoodsReceipt = EnumHelper.GetEnumDescription(AppActions.ViewGoodsReceipt);
    readonly string ActionGetTransactionTypes = EnumHelper.GetEnumDescription(AppActions.GetVendors);
    readonly string ActionGetWarehouses = EnumHelper.GetEnumDescription(AppActions.GetWarehouses);
    readonly string ActionGetItems = EnumHelper.GetEnumDescription(AppActions.GetAllItems);
    readonly string ActionGetBusinessPartners = EnumHelper.GetEnumDescription(AppActions.GetBusinessPartners);
    readonly string ActionCreateGoodsReceipt = EnumHelper.GetEnumDescription(AppActions.CreateGoodsReceipt);

    int BusinessPartnersCount { get; set; } = 0;
    int WarehousesCount { get; set; } = 0;

    #endregion Primitives

    #region Data Structures
    AppTable<GoodsReceiptLineVM> GoodsReceiptTable { get; set; } = default!;
    DataGridSettings GoodsReceiptTableSettings { get; set; } = new();
    List<BusinessPartnerVM> BusinessPartners { get; set; } = [];
    List<WarehouseVM> Warehouses { get; set; } = [];
    List<TransactionTypeVM> TransactionTypes { get; set; } = [];

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
        AppBusyService.SetBusy(ActionGetGoodsReceipt, true);
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
        if (FormData.DocumentLines.Count() <= 0)
        {
            ToastService.Warning("Please select Items to Receipt");
            return;
        }

        if (FormData.DocumentLines.All(x => x.Quantity <= 0))
        {
            ToastService.Warning("Please provide Quantities to the selected Items");
            return;
        }

        if (FormData.DocumentLines.Any(x => x.Quantity <= 0))
        {
            if (!await AlertService.PromptAsync("Some Items in the Goods Receipt has no Quantity. These Items will be removed in the transaction. Are you sure wou want to proceed?"))
                return;
            FormData.DocumentLines = [.. FormData.DocumentLines.ToList().Where(x => x.Quantity > 0)];
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateGoodsReceipt, true);

            bool response = await GoodsReceiptHandler.PostGoodsReceiptAsync(FormData);

            return response;
        }, AppActionOptionPresets.Confirmed(ActionCreateGoodsReceipt));

        action.OnSuccess(async (args) =>
        {
            NavManager.NavigateTo("/transactions/inventory/goods-receipt/?t=pndng");
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
        if (!AppBusyService.IsBusy(ActionGetGoodsReceipt))
        {
            AppBusyService.SetBusy(ActionGetGoodsReceipt, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await Task.WhenAll(
            GetGoodsReceipt(),
            LoadTransactionTypes(),
            LoadBusinessPartners(new()),
            LoadWarehouses(new()));

        FormData.PreparedBy = AuthenticationService.GetUserName();

        AppBusyService.SetBusy(ActionGetGoodsReceipt, false);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
    }

    async Task GetGoodsReceipt()
    {
        if (Creating)
            return;

        var action = await AppActionFactory.RunAsync(async () =>
        {
            GoodsReceiptVM? result = null;
            if (Draft == 1)
                result = await GoodsReceiptHandler.GetDraftGoodsReceiptAsync(Ref);
            else
                result = await GoodsReceiptHandler.GetGoodsReceiptAsync(Ref);

            AppBusyService.SetBusy(ActionGetGoodsReceipt, false);
            return result;

        }, AppActionOptionPresets.Loading(ActionGetGoodsReceipt));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Goods Receipt not found");
            else
            {
                action.Result.Adapt(FormData);

                FormData.PreparedBy = AuthenticationService.GetUserName();
            }
        });
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(GoodsReceiptTable.DataGrid, settings => GoodsReceiptTableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await GoodsReceiptTable.DataGrid.ReloadSettings();
        await GoodsReceiptTable.DataGrid.Reload();
    }

    async Task Receipt()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Goods Receipt Creation"))
                return;

        NavManager.NavigateTo($"/transactions/inventory/goods-receipt/?t=pndng", true);
    }

    async Task LoadTransactionTypes()
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetTransactionTypes, true);


            TransactionTypes = [.. await TransTypeHandler.GetTransactionTypesAsync()];

            AppBusyService.SetBusy(ActionGetTransactionTypes, false);
        }, AppActionOptionPresets.Loading(ActionGetTransactionTypes));
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

    void OnWarehouseChange(WarehouseVM? warehouse)
    {
        OnFieldChanged(nameof(FormData.Warehouse));

        if (warehouse is null)
            return;

        foreach (GoodsReceiptLineVM item in FormData.DocumentLines)
            item.Warehouse = warehouse;
    }

    void OnTransactionTypeChange(TransactionTypeVM? transType)
    {
        OnFieldChanged(nameof(FormData.TransactionType));

        if (transType is null)
            return;
    }

    void RemoveLine(GoodsReceiptLineVM item) => FormData.DocumentLines = [.. FormData.DocumentLines.Except([item])];

    async Task LoadBusinessPartners(LoadDataArgs args)
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            await Task.Yield();

            AppBusyService.SetBusy(ActionGetBusinessPartners, true);

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

            (IEnumerable<BusinessPartnerVM> Data, int Count) = await BusinessPartnerHandler.GetAllAsync(DatagridAdapter.QueryIntent);

            BusinessPartners = [.. Data];
            BusinessPartnersCount = Count;

            AppBusyService.SetBusy(ActionGetBusinessPartners, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetBusinessPartners));
    }

    async Task UpdateItemWarehouse(GoodsReceiptLineVM item)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            await Task.Yield();

            DatagridAdapter = new DataGridIntentAdapter(new());
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            (IEnumerable<ItemVM> Data, int Count) = await ItemMasterDataHandler.GetItemsInWarehouseAsync(DatagridAdapter.QueryIntent, item.Warehouse.WhsCode, [item.ItemCode]);

            if (Count > 0)
                item.OnHandQty = Data.First().Quantity;

            await GoodsReceiptTable.DataGrid.RefreshDataAsync();

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetItems));
    }

    #endregion Custom Functions
}
