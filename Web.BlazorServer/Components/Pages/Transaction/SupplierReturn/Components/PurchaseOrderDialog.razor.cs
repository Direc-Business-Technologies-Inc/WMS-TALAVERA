using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Shared.Libraries.ViewModel;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;

public partial class PurchaseOrderDialog
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;

    readonly string ActionGetList = "Get Purchase Orders";

    async Task<(IEnumerable<PurchaseOrderDataGridVM>, int)> PurchaseOrdersProvider(DataGridIntent intent)
    {
        intent.Filters.Add(DataGridFilterUtilities.Equal(nameof(PurchaseOrderDataGridVM.Status), "F"));
        return await returnHandler.GetPurchaseOrdersDataGridAsync(intent);
    }

    async Task SelectPO(PurchaseOrderDataGridVM item)
    {
        DialogService.Close(item.ReferenceNumber);
    }
}
