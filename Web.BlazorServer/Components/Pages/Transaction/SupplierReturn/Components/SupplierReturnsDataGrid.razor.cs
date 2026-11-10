using Microsoft.AspNetCore.Components;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Shared.Entities;
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

    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;
    DateTime? DateRangeStart;
    DateTime? DateRangeEnd;
    bool dateRangeApplied;
    PrintStatusView PrintStatusView = default!;
    bool initialCountRefreshRequested;
    bool BulkSubmissionBusy;

    QuickDataGrid<SupplierReturnDataGridVM> Grid { get; set; } = default!;
    readonly Dictionary<int, SupplierReturnDataGridVM> SelectedRows = new();
    List<SupplierReturnDataGridVM> VisibleRows = new();
    bool AllVisibleSelected => VisibleRows.Count > 0 && VisibleRows.All(row => SelectedRows.ContainsKey(row.Id));

    Task OnGridDataLoaded()
    {
        if (!dateRangeApplied)
        {
            var hasDates = PrintStatusFilter.TryGetDateRange(VisibleRows, row => row.Date, out var range);
            DateRangeStart = hasDates ? range.Start : null;
            DateRangeEnd = hasDates ? range.End : null;
        }
        if (initialCountRefreshRequested) return Task.CompletedTask;
        initialCountRefreshRequested = true;
        _ = InvokeAsync(async () =>
        {
            await Task.Yield();
            await PrintStatusView.RefreshCountsAsync();
        });
        return Task.CompletedTask;
    }

    void ToggleRow(SupplierReturnDataGridVM row, bool selected)
    {
        if (selected) SelectedRows[row.Id] = row;
        else SelectedRows.Remove(row.Id);
    }

    void ToggleAllVisible(bool selected)
    {
        foreach (var row in VisibleRows)
            ToggleRow(row, selected);
    }

    async Task OpenBulkFulfillConfirmation()
    {
        if (BulkSubmissionBusy || SelectedRows.Count < 2) return;
        BulkSubmissionBusy = true;
        // Copy display data and IDs before opening: dialog search and grid reloads cannot alter submission.
        var rows = SelectedRows.Values.Select(row => new SupplierReturnDataGridVM
        {
            Id = row.Id, ReferenceNumber = row.ReferenceNumber, VendorName = row.VendorName,
            Date = row.Date, CategoryName = row.CategoryName, PreparedBy = row.PreparedBy
        }).ToArray();
        var ids = rows.Select(row => row.Id).Distinct().ToArray();
        try
        {
            var confirmed = await DialogService.OpenAsync("Submit Bulk Fulfillment Request",
                ds => builder =>
            {
                builder.OpenComponent<BulkFulfillConfirmation>(0);
                builder.AddAttribute(1, nameof(BulkFulfillConfirmation.Rows), rows);
                builder.CloseComponent();
            },
            new() { Width = "700px" });
            if (confirmed is not true) return;
            var result = await returnHandler.SubmitBulkFulfillmentAsync(new(ids));
            if (result.Outcome == BulkFulfillmentOutcome.Submitted)
            {
                SelectedRows.Clear();
                if (result.ReferenceId.HasValue) ToastService.Success(result.Message);
                else ToastService.Warning(result.Message);
            }
            else if (result.Outcome == BulkFulfillmentOutcome.Uncertain)
                ToastService.Warning(result.Message);
            else
                ToastService.Error(result.Message);
        }
        catch (Exception)
        {
            ToastService.Error("The request could not be confirmed. Check NetSuite before submitting again, then refresh the grid or contact an administrator.");
        }
        finally
        {
            BulkSubmissionBusy = false;
        }
    }

    async Task<(IEnumerable<SupplierReturnDataGridVM>, int)> LoadDataAsync(DataGridIntent intent)
    {
        PrintStatusFilter.Apply(intent, SelectedPrintStatus);
        if (dateRangeApplied)
            PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd);

        var result = await returnHandler.GetReturnsDataGridAsync(intent);
        // Selection applies to the current page only; discard it when the grid loads another page or filter.
        SelectedRows.Clear();
        VisibleRows = result.Data.ToList();
        return (VisibleRows, result.Item2);
    }
    Task<int> PrintCountAsync(PrintStatus status) => PrintStatusFilter.CountAsync(status, async intent =>
    {
        if (dateRangeApplied)
            PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd);
        return await returnHandler.GetReturnsDataGridAsync(intent);
    });

    async Task DateRangeChanged((DateTime Start, DateTime End) range)
    {
        dateRangeApplied = true;
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
    Task RefreshPrintStatusAsync() => Task.WhenAll(Grid.Reload(), PrintStatusView.RefreshCountsAsync());
    async Task OnRowAction(SupplierReturnDataGridVM data)
    {
        if (RowAction.HasDelegate)
            await RowAction.InvokeAsync(data);
    }

    // Every Returns to Supplier grid row is sourced from the NetSuite transaction table, so a row
    // that is present is by definition printable. NetSuitePrint returns null for a defensive id <= 0.
    static string? PrintUrl(SupplierReturnDataGridVM data) => NetSuitePrint.VendorReturn(data.Id);

}
