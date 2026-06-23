using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

partial class ItemReceiptForm
{
    [Parameter] public ItemReceiptVM Data { get; set; } = new();
    [Parameter] public EditContext? EditContext { get; set; } = null;
    [Parameter] public EventCallback<ItemReceiptVM> OnValidSubmit { get; set; }
    [Inject] NavigationManager NavManager { get; set; } = default!;

    readonly List<DropDownItem> Categories = new List<DropDownItem>()
    {
        new() {Name = "Good", Value = false},
        new() {Name = "Confiscated", Value = true},
    };

    public void Submit()
    {
        if (OnValidSubmit.HasDelegate && EditContext is not null && EditContext.Validate())
        {
            OnValidSubmit.InvokeAsync(Data);
        }
    }

    void Return()
    {
        switch (Data.SourceType)
        {
            case ItemReceiptVM.SourceTypes.PurchaseOrder:
                NavManager.NavigateTo($"/transactions/purchasing/receiving/purchase-order/view?ref={Data.CreatedFrom}");
                break;
            case ItemReceiptVM.SourceTypes.TransferOrder:
                NavManager.NavigateTo($"/transactions/purchasing/receiving/transfer-order/view?ref={Data.CreatedFrom}");
                break;
            case ItemReceiptVM.SourceTypes.Returns:
                NavManager.NavigateTo($"/transactions/purchasing/receiving/returns/view?ref={Data.CreatedFrom}");
                break;
        }
    }

    private class DropDownItem()
    {
        public string Name { get; set; } = string.Empty;
        public bool Value { get; set; } = false;
    }
}

