using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class SupplierReturnForm
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IVendorHandler vendorHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Parameter][EditorRequired] public required SupplierReturnVM Model { get; set; }
    [Parameter][EditorRequired] public required EditContext EditContext { get; set; }
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
}
