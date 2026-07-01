using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn;

public partial class SupplierReturnCreate
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;

    readonly string ActionCreateSupplierReturn = "Create Return to Supplier";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        FormData.Memo = "Created via WMS";
        FormData.Date = DateTime.Now;
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
        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            await returnHandler.CreateSupplierReturnAsync(data);
        }, ActionCreateSupplierReturn);

        action.OnSuccess(async () =>
        {
            await Task.Delay(100);
            NavManager.NavigateTo(SupplierReturnRoutes.INDEX);
        }); 
    }
}
