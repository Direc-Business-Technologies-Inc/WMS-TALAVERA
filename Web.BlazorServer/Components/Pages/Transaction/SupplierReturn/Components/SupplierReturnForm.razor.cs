using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
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

    async Task<(IEnumerable<ReturnCategoryVM>, int)> CategoryProvider(DataGridIntent intent)
    {
        return await returnHandler.GetReturnCategories(intent);
    }

    async Task<(IEnumerable<ReturnStatusVM>, int)> StatusProvider(DataGridIntent intent)
    {
        return await returnHandler.GetReturnStatuses(intent);
    }

    async Task<(IEnumerable<LocationVM>, int)> LocationProvider(DataGridIntent intent)
    {
        return await locationHandler.GetLocationsAsync(intent);
    }
    

    async Task<(IEnumerable<VendorVM>, int)> VendorProvider(DataGridIntent intent)
    {
        return await vendorHandler.GetVendorsListAsync(intent);
    }

    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitsProvider(DataGridIntent intent, int itemId)
    {
        return await itemsHandler.GetItemUnits(itemId,intent);
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
