using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Shared.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class SupplierReturnsDataGrid
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Parameter] public EventCallback<SupplierReturnDataGridVM> RowAction { get; set; }

    readonly string ActionGetReturnsToSupplier = "Get Returns to Supplier";

    ReturnStatusVM? FilterStatus = null;
    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;

    QuickDataGrid<SupplierReturnDataGridVM> Grid { get; set; } = default!;

    async Task<(IEnumerable<SupplierReturnDataGridVM>, int)> LoadDataAsync(DataGridIntent intent)
    {
        PrintStatusFilter.Apply(intent, SelectedPrintStatus);
        if (FilterStatus is not null)
        {
            intent.Filters.Add(
                DataGridFilterUtilities.Contains(
                    nameof(SupplierReturnDataGridVM.StatusName),
                    FilterStatus.Name));
        }

        return await returnHandler.GetReturnsDataGridAsync(intent);
    }
    Task<int> PrintCountAsync(PrintStatus status) =>
        PrintStatusFilter.CountAsync(status, returnHandler.GetReturnsDataGridAsync);
    async Task PrintStatusChanged(PrintStatus status)
    {
        SelectedPrintStatus = status;
        await Grid.ReloadFirstPage();
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

    // Every Returns to Supplier grid row is sourced from the NetSuite transaction table, so a row
    // that is present is by definition printable. NetSuitePrint returns null for a defensive id <= 0.
    static string? PrintUrl(SupplierReturnDataGridVM data) => NetSuitePrint.VendorReturn(data.Id);

}
