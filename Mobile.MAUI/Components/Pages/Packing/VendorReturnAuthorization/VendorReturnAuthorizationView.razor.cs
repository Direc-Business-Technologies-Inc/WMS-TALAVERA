using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel.VendorReturnAuthorization;

namespace Mobile.MAUI.Components.Pages.Packing.VendorReturnAuthorization;

public partial class VendorReturnAuthorizationView
{
    List<VendorReturnAuthorizationVM> Data { get; set; } = [];

    AppAction<List<VendorReturnAuthorizationVM>> ActionGetVRA;

    protected override async Task OnInitializedAsync()
    {
        string userId = await AuthState.GetAuthenticatedUserId();
        ActionGetVRA = new AppAction<List<VendorReturnAuthorizationVM>>
        {
            Name = "GetVRA",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<VendorReturnAuthorizationVM>>("/Packing/VendorReturnAuthorization/PendingReturn");
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
        await ActionFactory.ExecuteAppActionAsync(ActionGetVRA);
    }

}