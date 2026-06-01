using Microsoft.AspNetCore.Components;

namespace Web.BlazorServer.Components.Pages.Transaction.SalesReturn;

public partial class SalesReturnPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string T { get; set; } = "sr";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (T is not null)
            SelectedTab = T.ToLower() == "srr" ? 1 : 0;
    }

    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        T = SelectedTab == 0 ? "sr" : "srr";
        NavManager.NavigateTo($"/transactions/sales/sales-return?T={T}");
    }
    #endregion Custom Functions
}
