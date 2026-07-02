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

        if (data.Lines.Count == 0)
        {
            ToastService.Error("Please add at least one line to the inventory transfer request.");
            return;
        }
        if (LinesNeedAssignment(data, out var lines))
        {
            ToastService.Error("Please assign inventory details to lines: " + string.Join(", ", lines.Select(l => l.ItemCode)));
            return;
        }

        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            await itrHandler.CreateInventoryTransferRequest(data);
        }, ActionCreate);
    }

    private bool LinesNeedAssignment(InventoryTransferRequestVM data, out List<InventoryTransferRequestLineVM> lines)
    {
        lines = [..data.Lines.Where(x => !x.IsAllAssigned)];
        return lines.Any();
    }

}
