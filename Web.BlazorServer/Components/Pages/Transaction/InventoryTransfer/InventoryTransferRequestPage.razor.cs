using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransfer;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransfer;

public partial class InventoryTransferRequestPage
{
    [SupplyParameterFromQuery, Parameter] public string T { get; set; } = "request";
    [Inject] IInventoryTransferHandler InventoryTransferHandler { get; set; } = default!;
    int SelectedTab { get; set; } = 0;
    string ActionGetPostedInventoryTransferRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPostedInventoryTransferRequests);
    string ActionGetInventoryTransferRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllInventoryTransferRequests);
    string ActionGetPendingInventoryTransferRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPendingInventoryTransferRequests);
    string ActionGetRejectedInventoryTransferRequests { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllRejectedInventoryTransferRequests);

    const string ITR_VIEW_ITEM_URI = "/transactions/inventory/inventory-transfer/inventory-transfer-request/view?ref={0}";
    const string ITR_CREATE_ITEM_URI = "/transactions/inventory/inventory-transfer/inventory-transfer-request/create";
    const string POSTED_ITR_VIEW_ITEM_URI = "/transactions/inventory/inventory-transfer/posted-inventory-transfer-request/view?ref={0}";
    const string PENDING_ITR_VIEW_ITEM_URI = "/transactions/inventory/inventory-transfer/pending-inventory-transfer-request/view?ref={0}";
    const string REJECTED_ITR_VIEW_ITEM_URI = "/transactions/inventory/inventory-transfer/rejected-inventory-transfer-request/view?ref={0}";

    private readonly string[] tabStrings = { "request", "pending", "rejected", "posted" };

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (T is not null)
        {
            SelectedTab = Array.IndexOf(tabStrings, T);
            if (SelectedTab == -1)
                SelectedTab = 0;
        }
    }

    void TabChanged()
    {
        T = tabStrings[SelectedTab];
        NavManager.NavigateTo($"/transactions/inventory/inventory-transfer?T={T}");
    }

    async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> PendingITRGetter(DataGridIntent intent)
    {
        return await InventoryTransferHandler.GetPendingInventoryTransferDataGridAsync(intent);
    }
    async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> PostedITRGetter(DataGridIntent intent)
    {
        return await InventoryTransferHandler.GetPostedInventoryTransferDataGridAsync(intent);
    }
    async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> ITRGetter(DataGridIntent intent)
    {
        return await InventoryTransferHandler.GetInventoryTransferDataGridAsync(intent);
    }

    async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> RejectedITRGetter(DataGridIntent intent)
    {
        return await InventoryTransferHandler.GetRejectedInventoryTransferDataGridAsync(intent);
    }
}
