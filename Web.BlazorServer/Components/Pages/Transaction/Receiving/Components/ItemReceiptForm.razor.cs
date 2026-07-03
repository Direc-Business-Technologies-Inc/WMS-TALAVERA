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

    Dictionary<ItemReceiptLineVM, decimal> LinesQuantityGoodTempBank = new();
    Dictionary<ItemReceiptLineVM, decimal> LinesQuantityBadTempBank = new();

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

    async Task BarcodeScanned((BarcodeVM barcode, bool IsGood) input)
    {
        var item = Data.Lines.Where(x => x.ItemId == input.barcode.Item?.Id).FirstOrDefault();
        if (item is null) throw new Exception("Item is not included in this document");

        decimal piecesToAdd = input.barcode.UoM?.ConversionRate ?? 0;
        decimal diff = piecesToAdd / item.UoMRate;

        if (input.IsGood)
        {
            if (item.QuantityGood + diff > item.QuantityPlanned) throw new Exception("Item count exceeds the planned quantity");

            item.QuantityGood += diff;
            LinesQuantityGoodTempBank[item] = LinesQuantityGoodTempBank.TryGetValue(item, out decimal value) ? value + diff : diff;
        }
        else
        {
            item.QuantityBad += diff;
            LinesQuantityBadTempBank[item] = LinesQuantityBadTempBank.TryGetValue(item, out decimal value) ? value + diff : diff;
        }
    }

    void ClearAddedBarcodes()
    {
        foreach (var item in LinesQuantityGoodTempBank.ToList())
        {
            item.Key.QuantityGood -= item.Value;
        }
        foreach (var item in LinesQuantityBadTempBank.ToList())
        {
            item.Key.QuantityBad -= item.Value;
        }

        LinesQuantityGoodTempBank.Clear();
        LinesQuantityBadTempBank.Clear();
    }

    private class DropDownItem()
    {
        public string Name { get; set; } = string.Empty;
        public bool Value { get; set; } = false;
    }
}

