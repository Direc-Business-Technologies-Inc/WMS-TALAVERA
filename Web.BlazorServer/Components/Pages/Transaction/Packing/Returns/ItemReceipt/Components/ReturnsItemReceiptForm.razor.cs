using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Web.BlazorServer.Components.Pages.Transaction.Others.BarcodeScanning;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.Returns.ItemReceipt.Components;

public partial class ReturnsItemReceiptForm
{
    [Parameter] public ReturnsItemReceiptPackingVM Data { get; set; } = new();
    [Parameter] public EditContext? EditContext { get; set; }
    [Parameter] public EventCallback<ReturnsItemReceiptPackingVM> OnValidSubmit { get; set; }
    [Inject] NavigationManager NavManager { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppTable<ReturnsItemReceiptLinePackingVM> LinesTable = default!;
    DataGridSettings TableSettings { get; set; } = new();

    BarcodeStore BarcodeStore = new();
    List<ReturnsItemReceiptLinePackingVM> FulfillableLines => [.. Data.Lines.Where(line => !line.IsComplete)];

    bool isPlannedGreaterOpen => Data.Lines.Any(line => line.QuantityAvailable < line.QuantityOpen);

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


    void ApplyBarcodes(bool bad)
    {
        if (!BarcodeStore.Any()) return;

        foreach (var item in BarcodeStore.Items)
        {
            var itemCount = BarcodeStore.CountItemQuantity(item);
            ReturnsItemReceiptLinePackingVM? itemLine;

            if (selectedItems.Any())
            {
                itemLine = Data.Lines.FirstOrDefault(x => x.ItemCode == selectedItems.First().ItemCode && x.LineNumber == selectedItems.First().LineNumber);
                //itemLine = Data.Lines[selectedItemIndex];
            }
            else
            {
                itemLine = Data.Lines.FirstOrDefault(x => x.ItemCode == item.ItemNumber);
            }

            if (itemLine != null)
            {
                if (bad)
                    itemLine.QuantityBad += itemCount;
                else
                    itemLine.QuantityGood += itemCount;
            }
        }

        BarcodeStore.Clear();
    }

    private IList<ReturnsItemReceiptLinePackingVM> selectedItems = new List<ReturnsItemReceiptLinePackingVM>();

    async Task OnRowClick(DataGridRowMouseEventArgs<ReturnsItemReceiptLinePackingVM> args)
    {
        if (selectedItems.Contains(args.Data))
        {
            selectedItems = new List<ReturnsItemReceiptLinePackingVM>();       // Unselect
        }
        else
        {
            selectedItems = new List<ReturnsItemReceiptLinePackingVM>();
            selectedItems = new List<ReturnsItemReceiptLinePackingVM> { args.Data }; // Select
        }
    }

    bool IsValidBarcode(BarcodeVM barcode, out string reason)
    {
        var line = Data.Lines.FirstOrDefault(x => x.ItemCode == barcode.Item?.ItemNumber);

        if (selectedItems.Count != 0)
        {
            line = selectedItems.FirstOrDefault(x => x.ItemCode == barcode.Item?.ItemNumber);
            //line = Model.Lines[selectedItemIndex];
        }

        if (line is null)
        {
            reason = $"The item {barcode.Item?.ItemNumber} does not exist in the current document";
            return false;
        }


        if (line.QuantityGood + line.QuantityBad + BarcodeStore.CountItemQuantity(barcode.Item?.Id ?? -1) + (barcode.UoM?.ConversionRate ?? 1) > line.QuantityOpen)
        {
            reason = $"The quantity of the item {line.ItemCode} exceeds the expected amount";
            return false;
        }

        reason = "";
        return true;
    }


}
