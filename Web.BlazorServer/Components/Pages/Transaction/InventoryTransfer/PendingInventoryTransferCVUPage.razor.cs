using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel;
using Sprache;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransfer;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer;

partial class PendingInventoryTransferCVUPage
{
    [SupplyParameterFromQuery]
    [Parameter]
    public int Ref { get; set; }

    readonly string ActionGetPendingInventoryTransferRequest = EnumHelper.GetEnumDescription(AppActions.ViewPendingInventoryTransferRequest);
    [Inject] IInventoryTransferHandler InventoryTransferHandler { get; set; } = default!;

    async Task<InventoryTransferCVUVM> GetPendingInventoryTransferRequest()
    {
        return await InventoryTransferHandler.GetPendingInventoryTransferRequestAsync(Ref);
    }

    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Pending Inventory Transfer Request",
        Position = 0,
        Icon = "assignment",
    }];
}
