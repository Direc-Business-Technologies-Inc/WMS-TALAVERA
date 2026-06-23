using Mapster;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn;

public partial class SupplierReturnView
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [SupplyParameterFromQuery] public string? Ref { get; set; } = null;

    readonly string ActionGetReturn = "Get Return to Supplier";
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetReturn);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            AppBusyService.SetBusy(ActionGetReturn, true);
            await InvokeAsync(StateHasChanged);
            await LoadDataAsync();
        }
    }

    async Task LoadDataAsync()
    {
        var action =  await AppActionFactory.RunLoadingAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetReturn, true); 

            if (Ref is null) throw new Exception("Please select a Return to Supplier");


            return await returnHandler.GetReturnAsync(Ref);

        }, ActionGetReturn);

        action.OnFailure(async(ex) =>
        {
            NavManager.NavigateTo(SupplierReturnRoutes.INDEX);
            ToastService.Error(ex.Message);
        });

        action.OnSuccess(async (response) =>
        {
            response.Adapt(FormData);
            await InvokeAsync(StateHasChanged);
        });
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
