using Microsoft.AspNetCore.Components;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving;

public partial class ReceivingPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string T { get; set; } = "po";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (T is not null)
            SelectedTab = T.ToLower() == "grpo" ? 1 : 0;
    }

    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        T = SelectedTab == 0 ? "po" : "grpo";
        NavManager.NavigateTo($"/transactions/purchasing/receiving?T={T}");
    }
    #endregion Custom Functions
}
