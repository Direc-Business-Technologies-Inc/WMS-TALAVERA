using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;

public partial class InventoryAdjustmentCreatePage
{
    [Inject] public IInventoryAdjustmentHandler? inventoryAdjustmentHandler { get; set; }
    [Inject] public AppAuthenticationService authService { get; set; } = default!;
    [SupplyParameterFromQuery] public string? Type { get; set; }
    bool IsIssue => Type is not null && Type.Equals("issue", StringComparison.OrdinalIgnoreCase);
    readonly string ActionCreateInventoryAdjustment = "Create Inventory Adjustment";

    async Task OnSubmit(InventoryAdjustmentVM data)
    {
        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            if (inventoryAdjustmentHandler is null) throw new Exception("No registered handler for inventory adjustment");

            await inventoryAdjustmentHandler.CreateInventoryAdjustmentAsync(data);
        }, ActionCreateInventoryAdjustment);

        action.OnSuccess(async () =>
        {
            await Task.Delay(100);
            NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX);
        });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            FormData.Date = DateTime.Now;
            FormData.Memo = "Created via WMS";
            var nsEmployee = authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeName");
            FormData.PreparedBy = string.IsNullOrEmpty(nsEmployee) ? "No Netsuite Account Registered" : nsEmployee;
            FormData.Category = new()
            {
                Name = IsIssue ? "Goods Issue" : "Goods Receipt",
                Id = -1
            };
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
