using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.VendorReturnAuthorization.ItemReceipt;

public partial class VendorReturnAuthorizationItemReceiptCreatePage
{
    [SupplyParameterFromQuery] public string? Ref { get; set; }
    [Inject] IVendorReturnAuthorizationItemReceiptPackingHandler ItemReceiptPackingHandler { get; set; } = default!;

    readonly string ActionGetItemReceiptSource = "Get Vendor Return Authorization Item Receipt Source";
    readonly string ActionCreateItemReceipt = "Create Vendor Return Authorization Item Receipt";

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
            if (string.IsNullOrWhiteSpace(Ref)) throw new InvalidOperationException("Please select a source for vendor return authorization item receipt");

            var result = await ItemReceiptPackingHandler.GetItemReceiptSourceAsync(Ref);
            if (result is null) throw new Exception($"Couldn't find the source for vendor return authorization item receipt: \"{Ref}\"");

            return result;
        }, AppActionOptionPresets.Loading(ActionGetItemReceiptSource));

        action.OnFailure(ex =>
        {
            ToastService.Error(ex.Message);
            NavManager.NavigateTo($"{PackingRoutes.Root}?tab=vendorreturnauthorization");
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

    async Task OnValidSubmit(VendorReturnAuthorizationItemReceiptPackingVM model)
    {
        AppBusyService.SetBusy(ActionCreateItemReceipt, true);
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunAsync(async () =>
        {
            await ItemReceiptPackingHandler.PostItemReceipt(model);
        }, AppActionOptionPresets.Loading(ActionCreateItemReceipt));

        action.OnSuccess(() =>
        {
            NavManager.NavigateTo($"{PackingRoutes.Root}?tab=vendorreturnauthorization", true);
            return Task.CompletedTask;
        });

        AppBusyService.SetBusy(ActionCreateItemReceipt, false);
    }

    protected override Task CancelEditing() => throw new NotImplementedException();
    protected override Task HandleSubmit() => throw new NotImplementedException();
    protected override Task InitializeEditing() => throw new NotImplementedException();
}
