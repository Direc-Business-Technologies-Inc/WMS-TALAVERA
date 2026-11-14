using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Web.BlazorServer.Components.Custom;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving;

public partial class ReceivingPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter] public string Tab { get; set; } = "purchaseorder";
    #endregion Parameters

    #region Primitives
    int SelectedTab { get; set; } = 0;
    readonly bool[] Mounted = [false, false, false];
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Tab is not null)
            SelectedTab = Tab.ToLowerInvariant() switch { 
                "transferorder" => 1, 
                "returns" => 2,
                _ => 0 };
        Mounted[SelectedTab] = true;
    }

    #endregion Overrides

    #region Custom Functions
    void TabChanged()
    {
        Mounted[SelectedTab] = true;
        Tab = SelectedTab switch { 
            1 => "transferorder", 
            2 => "returns", 
            _ => "purchaseorder" };
        NavManager.NavigateTo($"/transactions/purchasing/receiving?tab={Tab}");
    }
    #endregion Custom Functions
}
