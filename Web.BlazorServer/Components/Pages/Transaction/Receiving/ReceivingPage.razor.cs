using Microsoft.AspNetCore.Components;
using Shared.Entities;
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
        return receivingHandler.GetItemReceiptsDatagridAsync(intent);
    }
    string PrintableURL(ItemReceiptDataGridVM ir) => $"{PRINTABLE_URL}&id={ir.Id}";
    #endregion Custom Functions
}
