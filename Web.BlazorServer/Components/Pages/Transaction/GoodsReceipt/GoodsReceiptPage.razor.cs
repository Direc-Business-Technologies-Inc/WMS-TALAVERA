using Microsoft.AspNetCore.Components;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReceipt;

public partial class GoodsReceiptPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string T { get; set; } = "pndng";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (T is not null)
            SelectedTab = T.ToLower() == "pndng" ? 0 : T.ToLower() == "aprvd" ? 1 : 2;
    }

    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        T = SelectedTab == 0 ? "pndng" : SelectedTab == 1 ? "aprvd" : "rjct";
        NavManager.NavigateTo($"/transactions/inventory/goods-receipt?T={T}");
    }
    #endregion Custom Functions
}
