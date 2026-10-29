using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving;

public partial class ReceivingPage
{
    [Inject] IReceivingHandler receivingHandler { get; set; } = default!;
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string Tab { get; set; } = "purchaseorder";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    PrintStatus ReceiptPrintStatus = PrintStatus.ForPrinting;
    QuickDataGrid<ItemReceiptDataGridVM> ReceiptGrid { get; set; } = default!;
    PrintStatusView ReceiptPrintStatusView { get; set; } = default!;
    DateTime? ReceiptDateRangeStart;
    DateTime? ReceiptDateRangeEnd;
    const string PRINTABLE_URL = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1914&deploy=1&compid=11608969&ns-at=AAEJ7tMQcHYZi6TWg02Efsn9l54jzr_F0odqnZsroaLYQ2W7pXI";
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Tab is not null)
            SelectedTab = Tab.ToLowerInvariant() switch { 
                "transferorder" => 1, 
                "returns" => 2,
                "itemreceipts" => 3,
                _ => 0 };
    }

    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        Tab = SelectedTab switch { 
            1 => "transferorder", 
            2 => "returns", 
            3 => "itemreceipts",
            _ => "purchaseorder" };
        NavManager.NavigateTo($"/transactions/purchasing/receiving?tab={Tab}");
    }

    Task<(IEnumerable<ItemReceiptDataGridVM>, int)> ItemReceiptsProvider(DataGridIntent intent)
    {
        PrintStatusFilter.Apply(intent, ReceiptPrintStatus);
        PrintStatusFilter.ApplyDateRange(intent, ReceiptDateRangeStart, ReceiptDateRangeEnd);
        return LoadItemReceiptsAsync(intent);
    }
    async Task<(IEnumerable<ItemReceiptDataGridVM>, int)> LoadItemReceiptsAsync(DataGridIntent intent)
    {
        var result = await receivingHandler.GetItemReceiptsDatagridAsync(intent);
        if (!ReceiptDateRangeStart.HasValue && PrintStatusFilter.TryGetDateRange(result.Item1, row => row.Date, out var range))
        {
            ReceiptDateRangeStart = range.Start;
            ReceiptDateRangeEnd = range.End;
            await InvokeAsync(StateHasChanged);
            _ = InvokeAsync(async () =>
            {
                await Task.Yield();
                await ReceiptGrid.Reload();
                await ReceiptPrintStatusView.RefreshCountsAsync();
            });
        }
        return result;
    }
    Task<int> ReceiptCountAsync(PrintStatus status) => PrintStatusFilter.CountAsync(status, async intent =>
    {
        PrintStatusFilter.ApplyDateRange(intent, ReceiptDateRangeStart, ReceiptDateRangeEnd);
        return await receivingHandler.GetItemReceiptsDatagridAsync(intent);
    });
    async Task ReceiptDateRangeChanged((DateTime Start, DateTime End) range)
    {
        ReceiptDateRangeStart = range.Start;
        ReceiptDateRangeEnd = range.End;
        await ReceiptGrid.ReloadFirstPage();
        await ReceiptPrintStatusView.RefreshCountsAsync();
    }
    async Task ReceiptPrintStatusChanged(PrintStatus status)
    {
        ReceiptPrintStatus = status;
        await ReceiptGrid.ReloadFirstPage();
    }
    string PrintableURL(ItemReceiptDataGridVM ir) => $"{PRINTABLE_URL}&id={ir.Id}";
    #endregion Custom Functions
}
