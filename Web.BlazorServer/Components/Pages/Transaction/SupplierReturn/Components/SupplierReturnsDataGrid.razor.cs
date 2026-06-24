using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class SupplierReturnsDataGrid
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Parameter] public EventCallback<SupplierReturnDataGridVM> RowAction { get; set; }

    readonly string ActionGetReturnsToSupplier = "Get Returns to Supplier";

    ReturnStatusVM? FilterStatus = null;

    QuickDataGrid<SupplierReturnDataGridVM> Grid { get; set; } = default!;

    async Task<(IEnumerable<SupplierReturnDataGridVM>, int)> LoadDataAsync(DataGridIntent intent)
    {
        if (FilterStatus is not null)
        {
            intent.Filters.Add(
                DataGridFilterUtilities.Contains(
                    nameof(SupplierReturnDataGridVM.StatusName),
                    FilterStatus.Name));
        }

        return await returnHandler.GetReturnsDataGridAsync(intent);
    }
    async Task<(IEnumerable<ReturnStatusVM>, int)> StatusProvider(DataGridIntent intent)
    {
        return await returnHandler.GetReturnStatuses(intent);
    }

    async Task FilterSet(ReturnStatusVM? filter)
    {
        FilterStatus = filter;

        await Grid.Reload();
    }

    async Task OnRowAction(SupplierReturnDataGridVM data)
    {
        if (RowAction.HasDelegate)
            await RowAction.InvokeAsync(data);
    }

}
