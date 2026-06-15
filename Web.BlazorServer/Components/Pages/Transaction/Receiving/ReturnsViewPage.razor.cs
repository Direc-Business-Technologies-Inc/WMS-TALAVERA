using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving;

partial class ReturnsViewPage
{
    [SupplyParameterFromQuery] public string? Ref { get; set; }
    [Inject] public IReceivingHandler? receivingHandler { get; set; }

    ReturnsVM Model = new();

    readonly string ActionGetReturns = "Get Return";
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetReturns);
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            AppBusyService.SetBusy(ActionGetReturns, true);
            await InvokeAsync(StateHasChanged);
            await LoadDataAsync();
        }
    }

    async Task LoadDataAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrEmpty(Ref)) throw new InvalidOperationException("Please select a return");
            if (receivingHandler is null) throw new Exception("No handlers registered for returns");

            var res = await receivingHandler.GetReturnsAsync(Ref);

            if (res is null) throw new Exception($"Couldn't find the return \"{Ref}\"");
            return res;
        }, AppActionOptionPresets.Loading(ActionGetReturns));

        action.OnFailure(ex =>
        {
            NavManager.NavigateTo("/transactions/purchasing/receiving?tab=returns");
            return Task.CompletedTask;
        });

        action.OnSuccess(res =>
        {
            Model = res;
            return Task.CompletedTask;
        });

        await InvokeAsync(StateHasChanged);
    }

}
