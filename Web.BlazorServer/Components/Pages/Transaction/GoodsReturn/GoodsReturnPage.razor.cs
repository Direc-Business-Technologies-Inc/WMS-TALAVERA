using Microsoft.AspNetCore.Components;

namespace Web.BlazorServer.Components.Pages.Transaction.GoodsReturn;

public partial class GoodsReturnPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string T { get; set; } = "gr";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (T is not null)
            SelectedTab = T.ToLower() == "grr" ? 1 : 0;
    }

    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        T = SelectedTab == 0 ? "gr" : "grr";
        NavManager.NavigateTo($"/transactions/purchasing/goods-return?T={T}");
    }
    #endregion Custom Functions
}
