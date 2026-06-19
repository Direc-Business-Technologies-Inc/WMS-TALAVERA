using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment.Components.InventoryAdjustmentForm;

public partial class InventoryAdjustmentForm
{
    [Parameter][EditorRequired] public required InventoryAdjustmentVM Model { get; set; }
    [Parameter][EditorRequired] public EditContext EditContext { get; set; }
    [Parameter] public EventCallback<InventoryAdjustmentVM> OnSubmit { get; set; }
    [Parameter] public bool ReadOnly { get; set; } = false;

    [Inject] ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Inject] IBusinessAccountHandler accountHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    public string ActionGetSubsidiaries => "Get Subsidiaries";
    public string ActionGetLocations => "Get Locations";
    public string ActionGetAccounts => "Get Accounts";
    public string ActionGetItemUnits => "Get Item Units";
    public QuickVirtualizedDropdown<BusinessAccountVM> AccountDropdown { get; set; } = default!;
    public QuickVirtualizedDropdown<LocationVM> LocationDropdown { get; set; } = default!;

    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        return await subsidiaryHandler.GetSubsidiariesAsync(intent);
    }

    async Task<(IEnumerable<LocationVM>, int)> LocationProvider(DataGridIntent intent)
    {
        return await locationHandler.GetLocationsAsync(intent);
    }

    async Task<(IEnumerable<BusinessAccountVM>, int)> AccountProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);
        return await accountHandler.GetBusinessAccountsBySubsidiaryDataGridAsync(intent, Model.Subsidiary.Id);
    }

    async Task<(IEnumerable<ItemUnitVM>, int)> UnitsProvider(int itemid, DataGridIntent intent)
    {
        return await itemsHandler.GetItemUnits(itemid, intent);
    }

    async Task AddItems(List<ItemsVM> items) {
        throw new NotImplementedException();
    }

    Task DeleteLine(InventoryAdjustmentLineVM line) {
        Model.Lines.Remove(line);
        return Task.CompletedTask;
    }

    async Task OnValidSubmit()
    {
        if ( OnSubmit.HasDelegate)
        {
           await OnSubmit.InvokeAsync(Model);
        }
    }
}
