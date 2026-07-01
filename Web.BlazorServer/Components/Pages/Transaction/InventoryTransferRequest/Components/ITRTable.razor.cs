using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest.Components;

public partial class ITRTable
{
    [Inject] public IInventoryTransferRequestHandler itrHandler { get; set; } = default!;
    [Parameter] public EventCallback<InventoryTransferRequestDataGridVM> RowAction { get; set; }
    readonly string ActionGetList = "Get Inventory Transfer Requests";

    InventoryTransferRequestStatusVM? FilterStatus { get; set; }
    QuickDataGrid<InventoryTransferRequestDataGridVM> DataGrid { get; set; } = default!;

    async Task<(IEnumerable<InventoryTransferRequestDataGridVM>, int)> GetList(DataGridIntent intent)
    {
        if (FilterStatus is not null)
        {
            intent.Filters.Add(
                DataGridFilterUtilities.Equal(nameof(InventoryTransferRequestDataGridVM.StatusName), FilterStatus.Name)
            );
        }

        return await itrHandler.GetInventoryTransferRequestsDataGridAsync(intent);
    }

    async Task OnRowAction(InventoryTransferRequestDataGridVM data)
    {
        if (RowAction.HasDelegate) await RowAction.InvokeAsync(data);
    }

    async Task<(IEnumerable<InventoryTransferRequestStatusVM>, int)> StatusProvider(DataGridIntent intent)
    {
        return await itrHandler.GetInventoryTransferRequestsStatusesAsync(intent);
    }

    async Task FilterChanged()
    {
        await DataGrid.Reload();
    }
}
