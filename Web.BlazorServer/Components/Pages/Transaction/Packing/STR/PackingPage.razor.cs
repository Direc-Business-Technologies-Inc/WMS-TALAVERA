using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Pages.Transaction.Packing;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.STR;

partial class PackingPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string Tab { get; set; } = "stocktransferrequest";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SelectedTab = Tab?.ToLowerInvariant() switch
        {
            "returns" => 1,
            _ => 0
        };
    }

    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        Tab = SelectedTab switch
        {
            1 => "returns",
            _ => "stocktransferrequest"
        };
        NavManager.NavigateTo($"{PackingRoutes.Root}?tab={Tab}");
    }
    #endregion Custom Functions
}
