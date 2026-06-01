using Domain.Entities.Enums.Transaction.InventoryCounting;
using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting;

public partial class InventoryCountingCVUPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public Guid? Id { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] IInventoryCountingHandler InventoryCountingHandler { get; set; } = default!;
    [Inject] IWarehouseMasterDataHandler WarehouseHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }
    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsLoadingData => AppBusyService.IsBusy(ActionView);
    bool IsLoadingWarehouseItems => AppBusyService.IsBusy(ActionGetItems);
    bool IsEditableStatus => FormData.Status is InventoryCountingDocumentStatus.Open
                                             or InventoryCountingDocumentStatus.Recount;

    readonly string ActionView = EnumHelper.GetEnumDescription(AppActions.ViewInventoryCountingDocument);
    readonly string ActionCreate = EnumHelper.GetEnumDescription(AppActions.CreateInventoryCountingDocument);
    readonly string ActionGetItems = EnumHelper.GetEnumDescription(AppActions.GetWarehouseItemsForCounting);
    readonly string ActionGetWarehouses = EnumHelper.GetEnumDescription(AppActions.GetWarehouses);
    readonly string ActionSave = EnumHelper.GetEnumDescription(AppActions.SaveInventoryCountingDocument);
    readonly string ActionPost = EnumHelper.GetEnumDescription(AppActions.PostInventoryCountingDocument);
    readonly string ActionRecount = EnumHelper.GetEnumDescription(AppActions.RecountInventoryCountingDocument);

    int WarehousesCount { get; set; }
    IEnumerable<CycleType> CycleTypeValues { get; } = Enum.GetValues<CycleType>();
    #endregion Primitives

    #region Data Structures
    AppTable<InventoryCountingLineVM> DocumentLinesTable { get; set; } = default!;
    DataGridSettings DocumentLinesTableSettings { get; set; } = new();
    List<WarehouseVM> Warehouses { get; set; } = [];
    IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;
    #endregion Data Structures

    #region Overrides
    protected override void OnParametersSet()
    {
        PageAction = PageActionHelper.GetPageActionType(NavManager.Uri);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Viewing)
            AppBusyService.SetBusy(ActionView, true);
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
        if (!Id.HasValue) return;

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionView, true);
            return await InventoryCountingHandler.GetInventoryCountingDocumentAsync(Id.Value);
        }, AppActionOptionPresets.Loading(ActionView));

        AppBusyService.SetBusy(ActionView, false);

        action.OnSuccess(result =>
        {
            if (result is null)
            {
                ToastService.Error("Inventory Counting document not found.");
                return Task.CompletedTask;
            }
            result.Adapt(FormData);
            AdaptToClone();
            return Task.CompletedTask;
        });
    }

    protected override async Task CancelEditing()
    {
        AdaptToForm();
        GoBack();
    }

    protected override async Task HandleSubmit()
    {
        if (!FormData.DocumentLines.Any())
        {
            ToastService.Warning("Please add at least one item before submitting.");
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreate, true);
            return await InventoryCountingHandler.CreateInventoryCountingDocumentAsync(FormData);
        }, AppActionOptionPresets.Confirmed(ActionCreate));

        AppBusyService.SetBusy(ActionCreate, false);

        action.OnSuccess(async result =>
        {
            UnsavedChangesService.MarkClean();
            NavManager.NavigateTo("/transactions/inventory/inventory-counting?T=open", true);
        });
    }
    #endregion Overrides

    #region Custom Functions
    async Task LoadDataAsync()
    {
        GridSettingsLoaded = true;

        await LoadWarehouses(new());

        if (Viewing)
            await InitializeEditing();

        AppBusyService.SetBusy(ActionView, false);
        await InvokeAsync(StateHasChanged);
    }

    async Task LoadWarehouses(LoadDataArgs args)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetWarehouses, true);

            DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 10;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new AppFilterDescriptor
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

    void OnWarehouseChange()
    {
        OnFieldChanged(nameof(FormData.Warehouse));
        FormData.DocumentLines = [];
        _ = LoadWarehouseItemsAsync();
    }

    async Task LoadWarehouseItemsAsync()
    {
        if (string.IsNullOrEmpty(FormData.Warehouse?.WhsCode)) return;

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetItems, true);
            return await InventoryCountingHandler.GetWarehouseItemsForCountingAsync(FormData.Warehouse.WhsCode);
        }, AppActionOptionPresets.Loading(ActionGetItems));

        AppBusyService.SetBusy(ActionGetItems, false);

        action.OnSuccess(async result =>
        {
            FormData.DocumentLines = [.. (result ?? []).Where(x => x.Quantity > 0)];
            
            if (DocumentLinesTable is not null)
                await DocumentLinesTable.DataGrid.Reload();

            await InvokeAsync(StateHasChanged);
        });
    }

    void RemoveLine(InventoryCountingLineVM line) =>
        FormData.DocumentLines = [.. FormData.DocumentLines.Except([line])];

    async Task SaveDocument()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionSave, true);
            return await InventoryCountingHandler.SaveInventoryCountingDocumentAsync(FormData.Id);
        }, AppActionOptionPresets.Confirmed(ActionSave));

        AppBusyService.SetBusy(ActionSave, false);
        action.OnSuccess(async _ => await InitializeEditing());
    }

    async Task PostDocument()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionPost, true);
            return await InventoryCountingHandler.PostInventoryCountingDocumentAsync(FormData.Id);
        }, AppActionOptionPresets.Confirmed(ActionPost));

        AppBusyService.SetBusy(ActionPost, false);
        action.OnSuccess(async _ => await InitializeEditing());
    }

    async Task RecountDocument()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionRecount, true);
            return await InventoryCountingHandler.RecountInventoryCountingDocumentAsync(FormData.Id);
        }, AppActionOptionPresets.Confirmed(ActionRecount));

        AppBusyService.SetBusy(ActionRecount, false);
        action.OnSuccess(async _ => await InitializeEditing());
    }

    void AddSheet() =>
        NavManager.NavigateTo($"/transactions/inventory/inventory-counting/new-sheet?Document={FormData.Id}");

    async Task GoBack()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Inventory Counting Creation"))
                return;

        NavManager.NavigateTo("/transactions/inventory/inventory-counting?T=open", true);
    }

    void ExpandAllRows(RowRenderEventArgs<InventoryCountingLineVM> args)
    {
        args.Expandable = Viewing;

        if (args.Data.ActualQuantity == 0)
            return;

        var style = args.Data.ActualQuantity == args.Data.Quantity
            ? "background-color: rgba(40, 167, 69, 0.12);"
            : args.Data.ActualQuantity > args.Data.Quantity
                ? "background-color: rgba(255, 193, 7, 0.2);"
                : "background-color: rgba(220, 53, 69, 0.12);";

        args.Attributes["style"] = style;
    }
    #endregion Custom Functions
}
