using Microsoft.AspNetCore.Components;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting;

public partial class InventoryCountingPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string T { get; set; } = "open";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    #endregion Primitives

    #region Overrides
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (T is not null)
            SelectedTab = T.ToLower() == "history" ? 1 : 0;
    }
    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        T = SelectedTab == 0 ? "open" : "history";
        NavManager.NavigateTo($"/transactions/inventory/inventory-counting?T={T}");
    }
    #endregion Custom Functions
}
