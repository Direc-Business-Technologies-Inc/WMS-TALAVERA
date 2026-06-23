using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn;

public partial class SupplierReturnCreate
{
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
}
