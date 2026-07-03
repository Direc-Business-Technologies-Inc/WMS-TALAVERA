using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest.Components;

public partial class ITRForm
{
    [Inject] ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Parameter][EditorRequired] public InventoryTransferRequestVM Model { get; set; }
    [Parameter][EditorRequired] public EditContext EditContext { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnSubmit { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnReturn { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnSecondaryAction { get; set; }
    [Parameter] public string ReturnLabel { get; set; } = "Return";
    [Parameter] public string SubmitLabel { get; set; } = "Submit";
    [Parameter] public string SecondaryActionLabel { get; set; } = "Action";
    [Parameter] public bool ReadOnly { get; set; } = false;

    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        return await subsidiaryHandler.GetSubsidiariesAsync(intent);
    }
    async Task<(IEnumerable<LocationVM>, int)> SourceLocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }
    async Task<(IEnumerable<LocationVM>, int)> DestinationLocationProvider(DataGridIntent intent)
    {
        if (Model.SourceLocation is null) return ([], 0);

        return await locationHandler.GetSublocationsOfLocationAsync(intent, Model.SourceLocation.Id);
    }
    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitProvider(DataGridIntent intent, int itemId)
    {
        return await itemsHandler.GetItemUnits(itemId,intent);
    }

    async Task SecondaryAction()
    {
        if (OnSecondaryAction.HasDelegate) await OnSecondaryAction.InvokeAsync(Model);
    }

    async Task Return()
    {
        if (OnReturn.HasDelegate) await OnReturn.InvokeAsync(Model);
    }

    async Task Submit()
    {
        if (OnSubmit.HasDelegate) await OnSubmit.InvokeAsync(Model);
    }

}
