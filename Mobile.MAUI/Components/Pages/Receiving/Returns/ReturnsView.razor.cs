using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel.Returns;

namespace Mobile.MAUI.Components.Pages.Receiving.Returns;

public partial class ReturnsView
{
    List<ReturnsVM> Data { get; set; } = [];

    AppAction<List<ReturnsVM>> ActionGetReturns;

    protected override async Task OnInitializedAsync()
    {
        string userId = await AuthState.GetAuthenticatedUserId();
        ActionGetReturns = new AppAction<List<ReturnsVM>>
        {
            Name = "GetReturns",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<ReturnsVM>>("/Receiving/Returns/PendingReceipt");
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
        await ActionFactory.ExecuteAppActionAsync(ActionGetReturns);
    }
}