using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.VendorReturnAuthorization.ItemReceipt.Components;

public partial class VendorReturnAuthorizationItemReceiptForm
{
    [Parameter] public VendorReturnAuthorizationItemReceiptPackingVM Data { get; set; } = new();
    [Parameter] public EditContext? EditContext { get; set; }
    [Parameter] public EventCallback<VendorReturnAuthorizationItemReceiptPackingVM> OnValidSubmit { get; set; }
    [Inject] NavigationManager NavManager { get; set; } = default!;

    public void Submit()
    {
        if (OnValidSubmit.HasDelegate && EditContext is not null && EditContext.Validate())
        {
            OnValidSubmit.InvokeAsync(Data);
        }
    }

    void Return()
    {
        NavManager.NavigateTo(Data.SourceType switch
        {
            VendorReturnAuthorizationItemReceiptPackingVM.SourceTypes.VendorReturnAuthorization => $"{PackingRoutes.VendorReturnAuthorizationView}?ref={Data.CreatedFrom}",
            _ => $"{PackingRoutes.Root}?tab=vendorreturnauthorization"
        });
    }
}
