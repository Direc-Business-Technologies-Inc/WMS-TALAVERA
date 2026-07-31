using Mapster;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;

public partial class InventoryAdjustmentViewPage
{
    [SupplyParameterFromQuery] public string? Ref { get; set; } = null;
    [Inject] IInventoryAdjustmentHandler handler { get; set; } = default!;


    bool IsLoadingData => AppBusyService.IsBusy(ActionGetInventoryAdjustment);
    bool IsIssue = false;

    readonly string ActionGetInventoryAdjustment = "Get Inventory Adjustment";


    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (string.IsNullOrEmpty(Ref)) NavError("Please select a inventory adjustment from the list");
    }

    List<ViewModels.System.NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Inventory Adjustment Document",
        Position = 0,
        Icon = "assignment",
    }];


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            AppBusyService.SetBusy(ActionGetInventoryAdjustment, true);
            await InvokeAsync(StateHasChanged);
            await LoadDataAsync();
        }
    }


    async Task LoadDataAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrEmpty(Ref)) throw new Exception("Please select a inventory adjustment from the list");

            var response = await handler.GetInventoryAdjustmentAsync(Ref);

            if (response is null) throw new Exception($"Inventory adjustment \"{Ref}\" could not be found");

            return response;
        }, AppActionOptionPresets.Loading(ActionGetInventoryAdjustment));

        action.OnSuccess(async res =>
        {
            res.Adapt(FormData);
            IsIssue = FormData.IsIssue;
            AdditionalRoutes[0].Name = FormData.IsIssue ? "Goods Issue" : "Goods Receipt";
            await InvokeAsync(StateHasChanged);
        });

        action.OnFailure(ex =>
        {
            return Task.Delay(100).ContinueWith(_ =>
            {
                NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX, true);

            });
        });
    }
    Task OnReturn(InventoryAdjustmentVM data)
    {
        NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX);
        return Task.CompletedTask;
    }

    void NavError(string message)
    {
        ToastService.Error(message);
        NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX, true);
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }
}
