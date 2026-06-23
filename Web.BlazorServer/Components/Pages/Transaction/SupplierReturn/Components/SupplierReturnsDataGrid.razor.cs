using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class SupplierReturnsDataGrid
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Parameter] public EventCallback<SupplierReturnDataGridVM> RowAction { get; set; }

    readonly string ActionGetReturnsToSupplier = "Get Returns to Supplier";

    async Task<(IEnumerable<SupplierReturnDataGridVM>, int)> LoadDataAsync(DataGridIntent intent)
    {
        return await returnHandler.GetReturnsDataGridAsync(intent);
    }

    async Task OnRowAction(SupplierReturnDataGridVM data)
    {
        if (RowAction.HasDelegate)
            await RowAction.InvokeAsync(data);
    }

}
