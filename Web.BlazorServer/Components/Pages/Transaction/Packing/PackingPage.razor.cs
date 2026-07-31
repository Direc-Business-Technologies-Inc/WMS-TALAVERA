using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;
using Web.BlazorServer.ViewModels.Transaction.Packing;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing;

partial class PackingPage
{
    [Inject] IReturnPackingHandler packingHandler { get; set; } = default!;
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
            "vendorreturnauthorization" => 2,
            "packedfulfillments" => 3,
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
            2 => "vendorreturnauthorization",
            3 => "packedfulfillments",
            _ => "stocktransferrequest"
        };
        NavManager.NavigateTo($"{PackingRoutes.Root}?tab={Tab}");
    }
    #endregion Custom Functions
    public Task<(IEnumerable<PackedItemFulfillmentVM>, int)> ItemFulfillmentsProvider(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
        {
            intent.Sorts.Add(
                DataGridSortUtilities.Descending(nameof(PackedItemFulfillmentVM.DateLastModified))
            );
        }
        return packingHandler.GetPackedItemFulfillments(intent);
    }
    const string PRINTABLE_URL = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1922&deploy=1&compid=11608969&ns-at=AAEJ7tMQ70cbDMgsewbx6YHr0oQkl5HAZi1-qpSrLgdV9mevdZI";
    string PrintableURL(PackedItemFulfillmentVM Model) => $"{PRINTABLE_URL}&id={Model.Id}";

}
