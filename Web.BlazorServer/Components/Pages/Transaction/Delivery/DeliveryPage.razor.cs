using Microsoft.AspNetCore.Components;

namespace Web.BlazorServer.Components.Pages.Transaction.Delivery;

public partial class DeliveryPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string T { get; set; } = "so";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    #endregion Primitives

    #region Overrides
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (T is not null)
            SelectedTab = T.ToLower() == "dlv" ? 1 : 0;
    }
    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        T = SelectedTab == 0 ? "so" : "dlv";
        NavManager.NavigateTo($"/transactions/sales/delivery?T={T}");
    }
    #endregion Custom Functions
}
