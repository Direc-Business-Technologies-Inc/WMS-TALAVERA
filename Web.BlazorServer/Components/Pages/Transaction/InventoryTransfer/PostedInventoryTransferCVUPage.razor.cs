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

partial class PostedInventoryTransferCVUPage
{
    [SupplyParameterFromQuery]
    [Parameter] 
    public int Ref { get; set; }

    readonly string ActionGetPostedInventoryTransferRequest = EnumHelper.GetEnumDescription(AppActions.ViewPostedInventoryTransferRequest);
    [Inject] IInventoryTransferHandler InventoryTransferHandler { get; set; } = default!;

    async Task<InventoryTransferCVUVM> GetPostedInventoryTransferRequest()
    {
       return await InventoryTransferHandler.GetPostedInventoryTransferRequestAsync(Ref);
    }
    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Posted Inventory Transfer Request",
        Position = 0,
        Icon = "assignment",
    }];
}
