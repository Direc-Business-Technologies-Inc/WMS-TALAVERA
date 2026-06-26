using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel;

namespace Mobile.MAUI.Components.Pages.Receiving.TransferOrder;

public partial class TransferOrderView
{
    List<TransferOrderVM> Data { get; set; } = [];

    AppAction<List<TransferOrderVM>> ActionGetTransferOrder;

    protected override async Task OnInitializedAsync()
    {
        string userId = await AuthState.GetAuthenticatedUserId();
        ActionGetTransferOrder = new AppAction<List<TransferOrderVM>>
        {
            Name = "GetTransferOrder",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<TransferOrderVM>>("/TransferOrder/PendingReceipt");
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
        await ActionFactory.ExecuteAppActionAsync(ActionGetTransferOrder);
    }

}