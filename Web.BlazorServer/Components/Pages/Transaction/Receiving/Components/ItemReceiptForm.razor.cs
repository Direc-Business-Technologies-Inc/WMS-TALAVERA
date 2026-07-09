using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using static Web.BlazorServer.Components.Pages.Transaction.Receiving.Components.ScanDialog;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

partial class ItemReceiptForm
{
    [Parameter] public ItemReceiptVM Data { get; set; } = new();
    [Parameter] public EditContext? EditContext { get; set; } = null;
    [Parameter] public EventCallback<ItemReceiptVM> OnValidSubmit { get; set; }
    [Parameter] public bool Disabled { get; set; } = false;
    [Inject] IReceivingHandler receivingHandler { get; set; } = default!;

    Dictionary<string, BarcodeCollection> Barcodes { get; set; } = new();
    string BarcodeSearchTerm = string.Empty;
    bool BarcodeSearchDisabled = false;

    readonly List<DropDownItem> Categories = new List<DropDownItem>()
    {
        new() {Name = "Good", Value = false},
        new() {Name = "Confiscated", Value = true},
    };

    public void Submit()
    {
        if (OnValidSubmit.HasDelegate && EditContext is not null && EditContext.Validate())
        {
            Barcodes.Clear();
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

    async Task BarcodeScanned((BarcodeVM barcode, bool isGood) input)
    {
        var item = Data.Lines.Where(x => x.ItemId == input.barcode.Item?.Id).FirstOrDefault();
        if (item is null) throw new Exception($"Item {input.barcode.Item?.ItemNumber} is not included in this document");

        decimal piecesToAdd = input.barcode.UoM?.ConversionRate ?? 0;
        decimal diff = piecesToAdd / item.UoMRate;
        if (input.isGood && item.QuantityOpen < item.QuantityGood + diff)
            throw new Exception($"Scanned items exceed the expected quantity for item {item.ItemCode}");

        var key = input.barcode.Barcode;
        if (!Barcodes.ContainsKey(key))
        {
            Barcodes[key] = new BarcodeCollection()
            {
                Barcode = input.barcode,
                Line = item,
                GoodCount = 0,
                BadCount = 0
            };
        }

        if (input.isGood)
        {
            Barcodes[key].GoodCount++;
            Barcodes[key].Line.QuantityGood += diff;
            Barcodes[key].QuantityGood += diff;
        } 
        else
        {
            Barcodes[key].BadCount++;
            Barcodes[key].Line.QuantityBad += diff;
            Barcodes[key].QuantityBad += diff;
        }
    }

    void ClearAddedBarcodes()
    {
        foreach (var item in Barcodes.Values)
        {
            item.Line.QuantityBad -= item.QuantityBad;
            item.Line.QuantityGood -= item.QuantityGood;
        }

        Barcodes.Clear();
    }

    public class BarcodeCollection
    {
        public required BarcodeVM Barcode { get; init; } 
        public required ItemReceiptLineVM Line { get; init; }
        public int GoodCount { get; set; }
        public int BadCount { get; set; }
        public decimal QuantityGood { get; set; }
        public decimal QuantityBad { get; set; }
    }

    public async Task SearchBarcode()
    {
        if (BarcodeSearchDisabled || string.IsNullOrEmpty(BarcodeSearchTerm)) return;

        BarcodeSearchDisabled = true;
        await InvokeAsync(StateHasChanged);

        BarcodeVM? barcode;
        if (Barcodes.ContainsKey(BarcodeSearchTerm))
            barcode = Barcodes[BarcodeSearchTerm].Barcode;
        else
            barcode = await receivingHandler.GetBarcodeData(BarcodeSearchTerm);

        if (barcode is not null)
            await BarcodeScanned((barcode, true));

        BarcodeSearchTerm = string.Empty;
        BarcodeSearchDisabled = false;
        await InvokeAsync(StateHasChanged);
    }

    private class DropDownItem()
    {
        public string Name { get; set; } = string.Empty;
        public bool Value { get; set; } = false;
    }
}

