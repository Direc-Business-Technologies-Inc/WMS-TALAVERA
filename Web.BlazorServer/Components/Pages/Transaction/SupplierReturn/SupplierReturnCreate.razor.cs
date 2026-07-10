using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn;

public partial class SupplierReturnCreate
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Inject] AppAuthenticationService authService { get; set; } = default!;

    readonly string ActionCreateSupplierReturn = "Create Return to Supplier";

    bool IsBusy = false;
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        FormData.Memo = "Created via WMS";
        FormData.Date = DateTime.Now;
        var employeeName = authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeName");
        FormData.PreparedBy = string.IsNullOrEmpty(employeeName) ? "No Netsuite Account Registered" : employeeName;
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
    async Task Return(SupplierReturnVM _)
    {
        NavManager.NavigateTo(SupplierReturnRoutes.INDEX);
    }

    async Task Submit(SupplierReturnVM data)
    {
        IsBusy = true;
        await InvokeAsync(StateHasChanged);
        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            await returnHandler.CreateSupplierReturnAsync(data);
        }, ActionCreateSupplierReturn);

        action.OnSuccess(async () =>
        {
            await Task.Delay(100);
            NavManager.NavigateTo(SupplierReturnRoutes.INDEX);
        });

        IsBusy = false;
        await InvokeAsync(StateHasChanged);
    }
}
