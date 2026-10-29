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
    DateTime? DateRangeStart;
    DateTime? DateRangeEnd;
    PrintStatusView PrintStatusView = default!;

    QuickDataGrid<SupplierReturnDataGridVM> Grid { get; set; } = default!;

    async Task<(IEnumerable<SupplierReturnDataGridVM>, int)> LoadDataAsync(DataGridIntent intent)
    {
        PrintStatusFilter.Apply(intent, SelectedPrintStatus);
        PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd);
        if (FilterStatus is not null)
        {
            intent.Filters.Add(
                DataGridFilterUtilities.Contains(
                    nameof(SupplierReturnDataGridVM.StatusName),
                    FilterStatus.Name));
        }

        var result = await returnHandler.GetReturnsDataGridAsync(intent);
        if (!DateRangeStart.HasValue && PrintStatusFilter.TryGetDateRange(result.Data, row => row.Date, out var range))
        {
            DateRangeStart = range.Start;
            DateRangeEnd = range.End;
            await InvokeAsync(StateHasChanged);
            _ = InvokeAsync(async () =>
            {
                await Task.Yield();
                await Grid.Reload();
                await PrintStatusView.RefreshCountsAsync();
            });
        }
        return result;
    }
    Task<int> PrintCountAsync(PrintStatus status) => PrintStatusFilter.CountAsync(status, async intent =>
    {
        PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd);
        if (FilterStatus is not null)
            intent.Filters.Add(DataGridFilterUtilities.Contains(nameof(SupplierReturnDataGridVM.StatusName), FilterStatus.Name));
        return await returnHandler.GetReturnsDataGridAsync(intent);
    });

    async Task DateRangeChanged((DateTime Start, DateTime End) range)
    {
        DateRangeStart = range.Start;
        DateRangeEnd = range.End;
        await Grid.ReloadFirstPage();
        await PrintStatusView.RefreshCountsAsync();
    }
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
