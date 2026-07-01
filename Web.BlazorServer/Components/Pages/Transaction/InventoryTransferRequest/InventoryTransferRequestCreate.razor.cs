using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest;

public partial class InventoryTransferRequestCreate : BaseForm<InventoryTransferRequestVM>
{
    [Inject] IInventoryTransferRequestHandler itrHandler { get; set; } = default!;

    readonly string ActionCreate = "Create Inventory transfer request";

    protected override void OnParametersSet()
    {
        FormData.Date = DateTime.Now;
        FormData.Memo = "Created via WMS";
    }
    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    async Task OnReturn(InventoryTransferRequestVM _)
    {
        NavManager.NavigateTo(ITRRoutes.INDEX);
    }

    async Task OnSubmit(InventoryTransferRequestVM data)
    {
        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            await itrHandler.CreateInventoryTransferRequest(data);
        }, ActionCreate);
    }

}
