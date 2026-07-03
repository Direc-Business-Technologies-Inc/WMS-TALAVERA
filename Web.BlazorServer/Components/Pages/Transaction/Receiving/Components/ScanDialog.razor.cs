using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

public partial class ScanDialog
{
    [Parameter] public EventCallback<(BarcodeVM, bool)> OnBarcodeAdded { get; set; }
    [Inject] IReceivingHandler receivingHandler { get; set; } = default!;

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetBarcode);
    bool IsGood = true;

    readonly string ActionGetBarcode = "Get Barcode";

    BarcodeVM barcode = new();
    Dictionary<string, BarcodeCounter> barcodeBank = new();
    Dictionary<string, BarcodeVM> barcodeCache = new();
    string HelperString = "";
    public async Task AddBarcodeScan()
    {
        AppBusyService.SetBusy(ActionGetBarcode, true);
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunLoadingAsync(async () => {

            var data = barcodeCache.TryGetValue(barcode.Barcode, out BarcodeVM value) ?
                value :
                await receivingHandler.GetBarcodeData(barcode.Barcode);

            barcodeCache[barcode.Barcode] = data;
            if (data is null)
            {
                HelperString = "Couldn't find data for that barcode";
            }
            else
            {
                try
                {
                    await OnBarcodeAdded.InvokeAsync((data, IsGood));
                    if (barcodeBank.ContainsKey(data.Barcode))
                        barcodeBank[data.Barcode].Count++;
                    else
                        barcodeBank.Add(data.Barcode, new BarcodeCounter(data));

                    HelperString = string.Empty;
                }
                catch (Exception ex) 
                {
                    HelperString = ex.Message;
                }

            }
        }, ActionGetBarcode);
        AppBusyService.SetBusy(ActionGetBarcode, false);
        await InvokeAsync(StateHasChanged);
    }

    public class BarcodeCounter(BarcodeVM barcode, int count = 1)
    {
        public BarcodeVM Barcode { get; init;  } = barcode;
        public int Count { get; set; } = count;
    }

    public void Close(bool save)
    {
        DialogService.Close(save);
    }

}
