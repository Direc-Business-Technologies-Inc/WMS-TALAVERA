using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class SupplierReturnForm
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IVendorHandler vendorHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Inject] ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter][EditorRequired] public required SupplierReturnVM Model { get; set; }
    [Parameter][EditorRequired] public required EditContext EditContext { get; set; }

    [Parameter] public EventCallback<SupplierReturnVM> OnSecondaryAction { get; set; }
    [Parameter] public EventCallback<SupplierReturnVM> OnSubmit { get; set; }
    [Parameter] public EventCallback<SupplierReturnVM> OnReturn { get; set; }
    [Parameter] public string SecondaryActionString { get; set; } = "Action";
    [Parameter] public string SubmitString { get; set; } = "Submit";
    [Parameter] public string ReturnString { get; set; } = "Return";
    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public bool Disabled { get; set; } = false;

    QuickVirtualizedDropdown<LocationVM> LocationDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<SubsidiaryVM> SubsidiaryDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<ReturnStatusVM> StatusDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<PurchaseSubcategoryVM> PurchaseSubcategoryDropdown { get; set; } = default!;

    readonly string ActionGetPO = "Get Purchase Order";
    bool canSelectPO = true;
    bool IsEditable => !ReadOnly && Model.SourcePO is null;
    bool IsFromPo => Model.SourcePO is not null;
    bool IsDisabled => Disabled || LoadingPO;
    bool LoadingPO = false;

    readonly string[] StatusIdsNormal = ["A", "B"];
    readonly string[] StatusIdsBad = ["B"];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        canSelectPO = Model.SourcePO is null && Model.Id is null;
    }

    async Task<(IEnumerable<ReturnCategoryVM>, int)> CategoryProvider(DataGridIntent intent)
    {
        return await returnHandler.GetReturnCategories(intent);
    }

    async Task<(IEnumerable<ReturnStatusVM>, int)> StatusProvider(DataGridIntent intent)
    {

        intent.Filters.Add(
            DataGridFilterUtilities.In(
                nameof(ReturnStatusVM.Id), 
                Model.ReturnCategory?.Id == 1 ? StatusIdsBad : StatusIdsNormal));

        return await returnHandler.GetReturnStatuses(intent);
    }

    async Task<(IEnumerable<LocationVM>, int)> LocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);
        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }

    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        if (Model.Vendor is null) return ([], 0);
        return await subsidiaryHandler.GetSubsidiariesByVendorAsync(intent, Model.Vendor.Id);
    }

    async Task<(IEnumerable<VendorVM>, int)> VendorProvider(DataGridIntent intent)
    {
        return await vendorHandler.GetVendorsListAsync(intent);
    }

    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitsProvider(DataGridIntent intent, int itemId)
    {
        return await itemsHandler.GetItemUnits(itemId,intent);
    }

    async Task<(IEnumerable<PurchaseCategoryVM>, int)> PurchaseCategoryProvider(DataGridIntent intent)
    {
        return await returnHandler.GetPurchaseCategoriesAsync(intent);
    }

    async Task<(IEnumerable<PurchaseSubcategoryVM>, int)> PurchaseSubcategoryProvider(DataGridIntent intent)
    {
        if (Model.PurchaseCategory is null) return ([], 0);
        return await returnHandler.GetPurchaseSubCategoriesAsync(Model.PurchaseCategory, intent);
    }

    async Task PurchaseCategorySet(PurchaseCategoryVM? val)
    {
        if (Model.PurchaseCategory == val) return;
        Model.PurchaseCategory = val;
        Model.PurchaseSubcategory = null;

        PurchaseSubcategoryDropdown.Reset();
    }

    async Task CategorySet(ReturnCategoryVM? val)
    {
        if (val?.Id == null && (!Model.Status?.Id.Equals("B", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            Model.Status = null;
            StatusDropdown.Reset();
        }
        Model.ReturnCategory = val;
    }

    async Task SubsidiarySet(SubsidiaryVM? vm)
    {
        if (Model.Subsidiary == vm) return;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing subsidiaries will remove all added items");
            if (!response) return;
        }

        Model.Subsidiary = vm;
        Model.Location = null;
        LocationDropdown.Reset();

        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task LocationSet(LocationVM? vm)
    {
        if (Model.Location == vm) return;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing locations will remove all added items");
            if (!response) return;
        }

        Model.Location = vm;
        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task VendorSet(VendorVM? vm)
    {
        if (Model.Vendor == vm) return;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing vendors will remove all added items");
            if (!response) return;
        }

        Model.Vendor = vm;
        Model.Subsidiary = null;
        Model.Location = null;

        LocationDropdown.Reset();
        SubsidiaryDropdown.Reset();

        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task GetPurchaseOrder(string poRef)
    {
        LoadingPO = true;
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            var x = await returnHandler.GetReturnFromPurchaseOrderAsync(poRef);
            return x;
        }, ActionGetPO);

        action.OnSuccess(async (po) =>
        {
            po.Adapt(Model);
            canSelectPO = false;
        });

        LoadingPO = false;
        await InvokeAsync(StateHasChanged);
    }

    async Task ReturnClicked()
    {
        if (OnReturn.HasDelegate)
            await OnReturn.InvokeAsync(Model);
    }

    async Task SecondaryActionClicked()
    {
        if (OnSecondaryAction.HasDelegate)
            await OnSecondaryAction.InvokeAsync(Model);
    }

    async Task SubmitClicked()
    {
        if (OnSubmit.HasDelegate)
            await OnSubmit.InvokeAsync(Model);
    }

    async Task AddItems(List<ItemsVM> items)
    {
        if (Model.Location is null) return;

        canSelectPO = false;
        Model.Lines.AddRange(
            items.Select(x => new SupplierReturnLineVM
            {
                ItemId = x.Id,
                ItemCode = x.Name,
                ItemDescription = x.Description,
                UoM = x.StockUnit,
                Location = Model.Location,
                QuantityAlloted = 0
            }));
    }
}
