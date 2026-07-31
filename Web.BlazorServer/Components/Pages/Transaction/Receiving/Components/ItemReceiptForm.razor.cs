using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Radzen;
using Radzen.Blazor.Rendering;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Pages.Transaction.Others.BarcodeScanning;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

partial class ItemReceiptForm
{
    [Parameter] public ItemReceiptVM Data { get; set; } = new();
    [Parameter] public EditContext? EditContext { get; set; } = null;
    [Parameter] public EventCallback<ItemReceiptVM> OnValidSubmit { get; set; }
    [Parameter] public bool Disabled { get; set; } = false;
    [Inject] IReceivingHandler receivingHandler { get; set; } = default!;
    [Inject] TooltipService tooltipService { get; set; } = default!;

    readonly TooltipOptions LineDetailsTooltipOptions = new()
    {
        Position = TooltipPosition.Top
    };

    readonly List<DropDownItem> Categories = new List<DropDownItem>()
    {
        new() {Name = "Good", Value = false},
        new() {Name = "Confiscated", Value = true},
    };

    readonly List<AppFilterDescriptor> StatusFilters = [
        DataGridFilterUtilities.In(
            nameof(InventoryStatusVM.Id),
            new List<int> { 1, 3 })
    ];

    BarcodeStore BarcodeStore = new();

    public async Task Submit()
    {
        if (Data.Lines.Any(x => (!x.IsAllAssigned && x.IsReceived)))
        {
            ToastService.Error("Please assign inventory details to all lines");
            return;
        }
        if (!Data.Lines.Any(x => x.QuantityAlloted > 0))
        {
            ToastService.Error("Please assign a quantity to at least one line");
            return;
        }
        if (Data.Lines.Where(x => x.IsReceived).Count(x => x.QuantityOpen < x.QuantityAlloted) > 0)
        {
            ToastService.Error("Cannot receive more than the open quantity");
            return;
        }

        if (OnValidSubmit.HasDelegate && EditContext is not null && EditContext.Validate())
        {
            ApplyBarcodes();
            await OnValidSubmit.InvokeAsync(Data);
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
    decimal GetLineQuantity(ItemReceiptLineVM line) => line.QuantityAlloted + BarcodeStore.CountItemQuantity(line.ItemId) / line.UoMRate;

    void SetLineQuantity(ItemReceiptLineVM line, decimal amount)
    {
        var barcodeCount = BarcodeStore.CountItemQuantity(line.ItemId) / line.UoMRate;
        amount = Math.Max(Math.Min(line.QuantityOpen, amount), barcodeCount);

        decimal rawAmount = amount - barcodeCount;
        line.QuantityAlloted = rawAmount;
    }
    
    void ApplyBarcodes()
    {
        if (!BarcodeStore.Any()) return;

        foreach (var item in BarcodeStore.Items)
        {
            var itemCount = BarcodeStore.CountItemQuantity(item);
            var itemLine = Data.Lines.First(x => x.ItemId == item.Id);

            if (itemLine != null) itemLine.QuantityAlloted += itemCount / itemLine.UoMRate;
        }

        BarcodeStore.Clear();
    }
    async Task SetLineInventoryDetails(ItemReceiptLineVM line, List<InventoryDetailVM> details)
    {
        line.InventoryDetails = [.. details];

        var countGood = details.Where(x => x.Status?.Id == 1).Sum(x => x.QuantityAlloted);
        var countBad = details.Where(x => x.Status?.Id == 3).Sum(x => x.QuantityAlloted);

        line.DetailsTooltipText = $"{countGood} Good, {countBad} Bad";

        await InvokeAsync(StateHasChanged);
    }

    bool IsValidBarcode(BarcodeVM barcode, out string reason)
    {
        var line = Data.Lines.FirstOrDefault(x => x.ItemId == barcode.Item?.Id);
        if (line is null)
        {
            reason = $"The item {barcode.Item?.ItemNumber} does not exist in the current document";
            return false;
        }

        var itemCount = BarcodeStore.CountItemQuantity(line.ItemId) / line.UoMRate;
        var incomingCount = (barcode.UoM?.ConversionRate ?? 0) / line.UoMRate;
        if (line.QuantityOpen - line.QuantityAlloted - itemCount < incomingCount)
        {
            reason = $"The quantity of the item {line.ItemCode} exceeds the expected amount";
            return false;
        }

        reason = "";
        return true;
    }

    void ShowLineTooltip(ElementReference reference, ItemReceiptLineVM line)
    {
        if (!string.IsNullOrEmpty(line.DetailsTooltipText))
            tooltipService.Open(reference, line.DetailsTooltipText, LineDetailsTooltipOptions);
    }

    private class DropDownItem()
    {
        public string Name { get; set; } = string.Empty;
        public bool Value { get; set; } = false;
    }
}

