using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest.Components;

public partial class ITRTable
{
    [Inject] public IInventoryTransferRequestHandler itrHandler { get; set; } = default!;
    [Parameter] public EventCallback<InventoryTransferRequestDataGridVM> RowAction { get; set; }
    readonly string ActionGetList = "Get Inventory Transfer Requests";

    async Task<(IEnumerable<InventoryTransferRequestDataGridVM>, int)> GetList(DataGridIntent intent)
    {
        return await itrHandler.GetInventoryTransferRequestsDataGridAsync(intent);
    }

    async Task OnRowAction(InventoryTransferRequestDataGridVM data)
    {
        if (RowAction.HasDelegate) await RowAction.InvokeAsync(data);
    }
}
