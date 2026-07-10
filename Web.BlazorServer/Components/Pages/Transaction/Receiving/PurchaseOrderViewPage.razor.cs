using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving;

partial class PurchaseOrderViewPage
{
    [SupplyParameterFromQuery] public string? Ref { get; set; }
    [Inject] public IReceivingHandler? receivingHandler { get; set; }

    PurchaseOrderVM Model = new();

    readonly string ActionGetPurchaseOrder = "Get Purchase Order";
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetPurchaseOrder);

    List<NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Purchase Order",
        Position = 0,
        Icon = "assignment",
    }];
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            AppBusyService.SetBusy(ActionGetPurchaseOrder, true);
            await InvokeAsync(StateHasChanged);
            await LoadDataAsync();
        }
    }

    async Task LoadDataAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrEmpty(Ref)) throw new InvalidOperationException("Please select a purchase order");
            if (receivingHandler is null) throw new Exception("No handlers registered for purchase orders");

            var res = await receivingHandler.GetPurchaseOrderAsync(Ref);

            if (res is null) throw new Exception($"Couldn't find the purchase order \"{Ref}\"");
            return res;
        }, AppActionOptionPresets.Loading(ActionGetPurchaseOrder));

        action.OnFailure(ex =>
        {
            NavManager.NavigateTo("/transactions/purchasing/receiving");
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
