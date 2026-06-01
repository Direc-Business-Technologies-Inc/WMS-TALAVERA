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

partial class RejectedInventoryTransferCVUPage
{
    [SupplyParameterFromQuery]
    [Parameter]
    public int Ref { get; set; }

    readonly string ActionGetRejectedInventoryTransferRequest = EnumHelper.GetEnumDescription(AppActions.ViewRejectedInventoryTransferRequest);
    [Inject] IInventoryTransferHandler InventoryTransferHandler { get; set; } = default!;

    async Task<InventoryTransferCVUVM> GetRejectedInventoryTransferRequest()
    {
        return await InventoryTransferHandler.GetRejectedInventoryTransferRequestAsync(Ref);
    }
    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Rejected Inventory Transfer Request",
        Position = 0,
        Icon = "assignment",
    }];

}
