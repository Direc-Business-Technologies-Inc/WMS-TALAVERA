using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using static Web.BlazorServer.Components.Pages.Transaction.Receiving.Components.ItemReceiptForm;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

public partial class ScanDialog
{
    [Parameter] public EventCallback<(BarcodeVM, bool)> OnBarcodeAdded { get; set; }
    [Parameter] public Dictionary<string, BarcodeCollection> Barcodes { get; set; } = new();
    [Inject] IReceivingHandler receivingHandler { get; set; } = default!;

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetBarcode);
    bool IsGood = true;

    readonly string ActionGetBarcode = "Get Barcode";
    Dictionary<string, BarcodeVM?> barcodeCache = new();
    BarcodeVM barcode = new();
    string HelperString = "";
    public async Task AddBarcodeScan()
    {
        if (string.IsNullOrEmpty(barcode.Barcode))
        {
            HelperString = "Please enter a barcode";
            return;
        }

        AppBusyService.SetBusy(ActionGetBarcode, true);
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunLoadingAsync(async () => {

            var data = barcodeCache.TryGetValue(barcode.Barcode, out BarcodeVM? value) ?
                value :
                await receivingHandler.GetBarcodeData(barcode.Barcode);

            barcodeCache[barcode.Barcode] = data;
            if (data is null)
            {
                HelperString = $"Couldn't find data for barcode {barcode.Barcode}";
            }
            else
            {
                try
                {
                    await OnBarcodeAdded.InvokeAsync((data, IsGood));
                    HelperString = string.Empty;
                }
                catch (Exception ex) 
                {
                    HelperString = ex.Message;
                }
            }
            barcode.Barcode = string.Empty;
        }, ActionGetBarcode);
        AppBusyService.SetBusy(ActionGetBarcode, false);
        await InvokeAsync(StateHasChanged);
    }

    public void Close(bool save)
    {
        DialogService.Close(save);
    }

}
