using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel.InventoryCounting;

namespace Mobile.MAUI.Components.Pages.InventoryCounting;

public partial class InventoryCountingView
{
    List<InventoryCountingVM> Data { get; set; } = [];

    AppAction<List<InventoryCountingVM>> ActionGetInventoryCounting;

    protected override async Task OnInitializedAsync()
    {
        ActionGetInventoryCounting = new AppAction<List<InventoryCountingVM>>
        {
            Name = "GetInventoryCountings",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<InventoryCountingVM>>("/InventoryCounting/Started");
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
        await ActionFactory.ExecuteAppActionAsync(ActionGetInventoryCounting);
    }
}