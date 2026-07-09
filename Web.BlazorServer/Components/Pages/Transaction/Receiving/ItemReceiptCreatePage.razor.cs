using Mapster;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Transaction.Receiving;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving;

partial class ItemReceiptCreatePage
{
    [SupplyParameterFromQuery] public string? Ref { get; set; }
    [Inject] IReceivingHandler? receivingHandler { get; set; }
    [Inject] AppAuthenticationService authService { get; set; } = default!;

    readonly string ActionGetItemReceiptSource = "Get Item Receipt Source";
    readonly string ActionCreateItemReceipt = "Create Item Receipt";

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetItemReceiptSource);
    bool IsBusy = false;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        AppBusyService.SetBusy(ActionGetItemReceiptSource, true);
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (string.IsNullOrEmpty(Ref)) throw new InvalidOperationException("Please select a source for item receipt");
            if (receivingHandler is null) throw new Exception("No handlers registered for item receipt");

            var res = await receivingHandler.GetItemReceiptSourceAsync(Ref);

            if (res is null) throw new Exception($"Couldn't find the source for item receipt: \"{Ref}\"");
            return res;
        }, AppActionOptionPresets.Loading(ActionGetItemReceiptSource));

        action.OnFailure(ex =>
        {
            NavManager.NavigateTo("/transactions/purchasing/receiving");
            return Task.CompletedTask;
        });

        action.OnSuccess(res =>
        {
            res.Adapt(FormData);
            var nsEmployee = authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeName");
            FormData.PreparedBy = string.IsNullOrEmpty(nsEmployee) ? "No Netsuite Account Registered" : nsEmployee;

            return Task.CompletedTask;
        });

        await InvokeAsync(StateHasChanged);
    }

    async Task OnValidSubmit(ItemReceiptVM model)
    {

        IsBusy = true;
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (receivingHandler is null) throw new Exception("No handlers registered for item receipt");
            return await receivingHandler.PostItemReceipt(model);

        }, AppActionOptionPresets.Confirmed(ActionGetItemReceiptSource));

        action.OnSuccess(async (res) =>
        {
            await Task.Delay(100);

            NavManager.NavigateTo("/transactions/purchasing/receiving");
        });

        IsBusy = false;
        await InvokeAsync(StateHasChanged);

    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }
}
