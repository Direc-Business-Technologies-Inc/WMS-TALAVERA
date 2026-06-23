using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;

public partial class InventoryAdjustmentCreatePage
{
    [Inject] public IInventoryAdjustmentHandler? inventoryAdjustmentHandler { get; set; }

    readonly string ActionCreateInventoryAdjustment = "Create Inventory Adjustment";
    async Task OnSubmit(InventoryAdjustmentVM data)
    {
        var action = AppActionFactory.RunConfirmedAsync(async () =>
        {
            if (inventoryAdjustmentHandler is null) throw new Exception("No registered handler for inventory adjustment");

            await inventoryAdjustmentHandler.CreateInventoryAdjustmentAsync(data);
        }, ActionCreateInventoryAdjustment);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            FormData.Date = DateTime.Now;
            FormData.Memo = "Created via WMS";
            await InvokeAsync(StateHasChanged);
        }
    }

    Task OnReturn(InventoryAdjustmentVM data)
    {
        NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX);
        return Task.CompletedTask;
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }
}
