using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment.Components.InventoryAdjustmentForm;

public partial class InventoryAdjustmentForm
{
    [Parameter][EditorRequired] public required InventoryAdjustmentVM Model { get; set; }
    [Parameter][EditorRequired] public EditContext EditContext { get; set; }
    [Parameter] public EventCallback<InventoryAdjustmentVM> OnSubmit { get; set; }
    [Parameter] public EventCallback<InventoryAdjustmentVM> OnSecondaryActionClicked { get; set; }
    [Parameter] public EventCallback<InventoryAdjustmentVM> OnReturnClicked { get; set; }
    [Parameter] public string ReturnString { get; set; } = "Return";
    [Parameter] public string SubmitString { get; set; } = "Submit";
    [Parameter] public string SecondaryActionString { get; set; } = "Action";
    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public bool Issue { get; set; } = false;

    [Inject] ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Inject] IBusinessAccountHandler accountHandler { get; set; } = default!;
    [Inject] IInventoryAdjustmentHandler adjustmentHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    public string ActionGetSubsidiaries => "Get Subsidiaries";
    public string ActionGetLocations => "Get Locations";
    public string ActionGetAccounts => "Get Accounts";
    public string ActionGetItemUnits => "Get Item Units";
    public string ActionGetReasons => "Get Inventory Adjustment Reasons";
    public QuickVirtualizedDropdown<BusinessAccountVM> AccountDropdown { get; set; } = default!;
    public QuickVirtualizedDropdown<LocationVM> LocationDropdown { get; set; } = default!;

    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        return await subsidiaryHandler.GetSubsidiariesAsync(intent);
    }

    async Task<(IEnumerable<LocationVM>, int)> LocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }

    async Task<(IEnumerable<BusinessAccountVM>, int)> AccountProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);
        return await accountHandler.GetBusinessAccountsBySubsidiaryDataGridAsync(intent, Model.Subsidiary.Id);
    }

    async Task<(IEnumerable<InventoryAdjustmentReasonVM>, int)> ReasonProvider(DataGridIntent intent)
    {
        return await adjustmentHandler.GetInventoryAdjustmentReasonsAsync(intent);
    }

    async Task<(IEnumerable<ItemUnitVM>, int)> UnitsProvider(int itemid, DataGridIntent intent)
    {
        return await itemsHandler.GetItemUnits(itemid, intent);
    }

    async Task SetReason(InventoryAdjustmentReasonVM? reason)
    {
        Model.Reason = reason;
        if (reason != null)
        {
            Model.Account = new BusinessAccountVM
            {
                Name = reason.AccountName,
                Id = reason.AccountId
            };
        }

        await InvokeAsync(StateHasChanged);
    }

    async Task SetSubsidiary(SubsidiaryVM? value)
    {
        Model.Subsidiary = value;
        LocationDropdown.Reset();
        AccountDropdown.Reset();
    }

    async Task AddItems(List<ItemsVM> items) {
        Model.Lines.AddRange(items.Select(x =>
            new InventoryAdjustmentLineVM
            {
                ItemId = x.Id,
                Type = Issue ? InventoryAdjustmentLineVM.Types.Issue : InventoryAdjustmentLineVM.Types.Receipt,
                ItemCode = x.ItemNumber,
                ItemDescription = x.Name,
                UoM = x.StockUnit,
                Location = Model.Location,
                QuantityOnHand = x.QuantityOnHand
            }
        ));
        await InvokeAsync(StateHasChanged);
    }

    Task DeleteLine(InventoryAdjustmentLineVM line) {
        Model.Lines.Remove(line);
        return Task.CompletedTask;
    }

    async Task SecondaryActionClicked()
    {
        if (OnSecondaryActionClicked.HasDelegate) await OnSecondaryActionClicked.InvokeAsync(Model);
    }

    async Task ReturnClicked()
    {
        if (OnReturnClicked.HasDelegate) await OnReturnClicked.InvokeAsync(Model);
    }

    async Task OnValidSubmit()
    {
        if (Model.Lines.Sum(x => x.QuantityAlloted) <= 0)
        {
            ToastService.Error("Please assign alloted quantities to items");
            return;
        } 

        if (!Model.Lines.Aggregate(true, (a, b) => a && b.IsAllAssignedToBins))
        {
            ToastService.Error("Please set inventory assignment details");
            return;
        } 
        if ( OnSubmit.HasDelegate)
        {
           await OnSubmit.InvokeAsync(Model);

        }
    }
}
