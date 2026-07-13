using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.Returns.ItemReceipt.Components;

public partial class ReturnsItemReceiptForm
{
    [Parameter] public ReturnsItemReceiptPackingVM Data { get; set; } = new();
    [Parameter] public EditContext? EditContext { get; set; }
    [Parameter] public EventCallback<ReturnsItemReceiptPackingVM> OnValidSubmit { get; set; }
    [Inject] NavigationManager NavManager { get; set; } = default!;

    List<ReturnsItemReceiptLinePackingVM> FulfillableLines => [.. Data.Lines.Where(line => !line.IsComplete)];

    public async Task Submit()
    {
        if (OnValidSubmit.HasDelegate && EditContext is not null && EditContext.Validate())
            await OnValidSubmit.InvokeAsync(Data);
    }

    void Return()
    {
        NavManager.NavigateTo(Data.SourceType switch
        {
            ReturnsItemReceiptPackingVM.SourceTypes.TransferOrder => $"{PackingRoutes.ReturnsView}?ref={Data.CreatedFrom}",
            _ => $"{PackingRoutes.Root}?tab=returns"
        });
    }
}
