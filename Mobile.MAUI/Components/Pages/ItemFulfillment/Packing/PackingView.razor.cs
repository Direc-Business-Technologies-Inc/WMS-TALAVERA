using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel;

namespace Mobile.MAUI.Components.Pages.ItemFulfillment.Packing;

public partial class PackingView
{
    List<PackingVM> Data { get; set; } = [];

    AppAction<List<PackingVM>> ActionGetPacking;

    protected override async Task OnInitializedAsync()
    {
        string userId = await AuthState.GetAuthenticatedUserId();
        ActionGetPacking = new AppAction<List<PackingVM>>
        {
            Name = "GetPacking",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<PackingVM>>("/Packing/PendingFulfillment");
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
        await ActionFactory.ExecuteAppActionAsync(ActionGetPacking);
    }
}