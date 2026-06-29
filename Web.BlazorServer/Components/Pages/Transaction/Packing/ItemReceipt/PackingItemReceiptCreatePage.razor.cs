using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.ItemReceipt;
using Web.BlazorServer.ViewModels.Transaction.Packing.ItemReceipt;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.ItemReceipt;

partial class PackingItemReceiptCreatePage
{
    [SupplyParameterFromQuery] public string? Ref { get; set; }
    [Inject] IItemReceiptPackingHandler ItemReceiptPackingHandler { get; set; } = default!;

    readonly string ActionGetItemReceiptSource = "Get Packing Item Receipt Source";
    readonly string ActionCreateItemReceipt = "Create Packing Item Receipt";

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetItemReceiptSource);

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
            if (string.IsNullOrWhiteSpace(Ref)) throw new InvalidOperationException("Please select a source for packing item receipt");

            var result = await ItemReceiptPackingHandler.GetItemReceiptSourceAsync(Ref);
            if (result is null) throw new Exception($"Couldn't find the source for packing item receipt: \"{Ref}\"");

            return result;
        }, AppActionOptionPresets.Loading(ActionGetItemReceiptSource));

        action.OnFailure(ex =>
        {
            ToastService.Error(ex.Message);
            NavManager.NavigateTo($"{PackingRoutes.Root}?tab=stocktransferrequest");
            return Task.CompletedTask;
        });

        action.OnSuccess(async result =>
        {
            FormData = result;
            await ResetFormContext();
        });

        AppBusyService.SetBusy(ActionGetItemReceiptSource, false);
        await InvokeAsync(StateHasChanged);
    }

    async Task OnValidSubmit(ItemReceiptPackingVM model)
    {
        AppBusyService.SetBusy(ActionCreateItemReceipt, true);
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunAsync(async () =>
        {
            await ItemReceiptPackingHandler.PostItemReceipt(model);
        }, AppActionOptionPresets.Loading(ActionCreateItemReceipt));

        action.OnSuccess(() =>
        {
            NavManager.NavigateTo($"{PackingRoutes.Root}?tab=stocktransferrequest", true);
            return Task.CompletedTask;
        });

        AppBusyService.SetBusy(ActionCreateItemReceipt, false);
    }

    protected override Task CancelEditing() => throw new NotImplementedException();
    protected override Task HandleSubmit() => throw new NotImplementedException();
    protected override Task InitializeEditing() => throw new NotImplementedException();
}
