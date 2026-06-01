using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Transaction.GoodsReturn;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.GoodsReturn;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.Components.Pages.Transaction.InventoryCounting.Components;
using Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

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

    protected override async Task InitializeEditing()
    {
        NavManager.NavigateTo($"/transactions/purchasing/receiving/purchase-order/create?ref={FormData.SapReference.DocEntry}", true);
    }

    protected override async Task CancelEditing()
    {
        if (UnsavedChangesService.HasChanges)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Goods Receipt PO Creation"))
                return;

        NavManager.NavigateTo($"/transactions/purchasing/receiving/purchase-order/view?ref={FormData.SapReference.DocEntry}", true);

    }

    protected override async Task HandleSubmit()
    {
        if (!FormData.Validate())
        {
            ToastService.Warning("Goods Receipt PO posting must have at least one Item with at least one Quantity");
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateGoodsReceiptPO, true);

            bool result = await ReceivingHandler.PostGoodsReceiptPOAsync(FormData);

            AppBusyService.SetBusy(ActionCreateGoodsReceiptPO, true);

            return result;
        }, AppActionOptionPresets.Confirmed(ActionCreateGoodsReceiptPO));

        action.OnSuccess(async (args) =>
        {
            await LoadDataAsync();
            NavManager.NavigateTo($"/transactions/purchasing/receiving/purchase-order/view?ref={FormData.SapReference.DocEntry}");
        });

    }

    #endregion Overrides

    #region Custom Function

    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetPurchaseOrder))
        {
            AppBusyService.SetBusy(ActionGetPurchaseOrder, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await Task.WhenAll(
            GetPurchaseOrder(),
            LoadReturnTypes(),
            LoadSchoolYears(new()));

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

                if (Creating)
                {
                    FormData.ReceivedBy = AuthenticationService.GetUserName();
                    FormData.PreparedBy = AuthenticationService.GetUserName();
                }
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

    async Task RemoveLine(PurchaseOrderLineVM line)
    {
        if (!await AlertService.PromptAsync())
            return;

        FormData.DocumentLines.Remove(line);
        await PurchaseOrderTable.DataGrid.RefreshDataAsync();
    }

    async Task AddFreeLine(PurchaseOrderLineVM line)
    {
        if (!await AlertService.PromptAsync())
            return;

        PurchaseOrderLineVM freeLine = line.Adapt<PurchaseOrderLineVM>();
        freeLine.TargetQuantity = 0;
        freeLine.OpenQuantity = 0;
        freeLine.Quantity = 0;
        freeLine.Price = 0;
        freeLine.Free = true;
        freeLine.DocEntry = 0;
        freeLine.LineNum = 0;

        FormData.DocumentLines.Insert(FormData.DocumentLines.IndexOf(line) + 1, freeLine);
        await PurchaseOrderTable.DataGrid.RefreshDataAsync();
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

    async Task OpenScannerDialog()
    {
        await DialogService.OpenAsync<InventoryCountingScanner>(
            "Scan Barcode",
            new Dictionary<string, object>
            {
                { "OnScan", EventCallback.Factory.Create<string>(this, HandleScanResult) }
            },
            options: new Radzen.DialogOptions
            {
                Width = "400px",
                CloseDialogOnOverlayClick = false,
            });
    }

    async Task HandleScanResult(string isbn)
    {
        var matchingLines = FormData.DocumentLines.Where(l =>
            !string.IsNullOrWhiteSpace(l.ISBN) &&
            l.ISBN.Equals(isbn, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matchingLines.Count == 0)
        {
            ToastService.Warning($"No item found for barcode: {isbn}");
            return;
        }

        PurchaseOrderLineVM? selectedLine = null;

        if (matchingLines.Count == 1)
        {
            selectedLine = matchingLines.First();
        }
        else
        {
            selectedLine = await DialogService.OpenAsync<ItemSelectionDialog>(
                "Select Item",
                new Dictionary<string, object> { { "Items", matchingLines } },
                new Radzen.DialogOptions { Width = "600px" });
        }

        if (selectedLine != null)
        {
            if (selectedLine.Free == false && selectedLine.Quantity >= selectedLine.TargetQuantity)
            {
                ToastService.Warning($"Item {selectedLine.ItemCode} has already reached its planned quantity.");
                return;
            }

            selectedLine.Quantity += 1;
            await PurchaseOrderTable.DataGrid.Reload();
            await InvokeAsync(StateHasChanged);
            
            ToastService.Success($"Incremented quantity for {selectedLine.ItemCode}");
        }
    }

    #endregion Custom Function


}
