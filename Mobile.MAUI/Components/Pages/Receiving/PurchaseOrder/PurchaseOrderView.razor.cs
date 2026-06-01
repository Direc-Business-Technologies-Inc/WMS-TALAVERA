using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel;

namespace Mobile.MAUI.Components.Pages.Receiving.PurchaseOrder
{
    public partial class PurchaseOrderView
    {
        List<PurchaseOrderVM> Data { get; set; } = [];

        AppAction<List<PurchaseOrderVM>> ActionGetPurchaseOrder;

        protected override async Task OnInitializedAsync()
        {
            string userId = await AuthState.GetAuthenticatedUserId();
            ActionGetPurchaseOrder = new AppAction<List<PurchaseOrderVM>>
            {
                Name = "GetPurchaseOrder",
                TaskAsync = async () =>
                {
                    await InvokeAsync(StateHasChanged);
                    var res = await Client.Get<List<PurchaseOrderVM>>("/Receiving/PO/PendingReceipt");
                    return res;
                },  
                OnSuccess = async (result) =>
                {
                    Data = result.Data ?? new();
                    await InvokeAsync(StateHasChanged);
                }
            };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LoadData();
            }
        }

        async Task LoadData()
        {
            await ActionFactory.ExecuteAppActionAsync(ActionGetPurchaseOrder);
        }

    }
}