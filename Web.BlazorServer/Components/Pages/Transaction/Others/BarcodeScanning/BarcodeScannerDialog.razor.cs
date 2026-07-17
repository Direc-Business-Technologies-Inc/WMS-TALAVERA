using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Others.BarcodeScanning;

public partial class BarcodeScannerDialog
{
    [Inject] IReceivingHandler receivingHandler { get; set; } = default!;
    [Parameter][EditorRequired] public BarcodeStore BarcodeStore { get; set; }
    [Parameter] public BarcodeVerifier? Verifier { get; set; }

    private BarcodeVM barcode = new();
    private HashSet<string> FailedBarcodesCache = new();

    const string ALERT_BARCODE_NOT_FOUND = "Couldn't find data for barcode {0}";
    const string ALERT_BARCODE_SUCCESS = "";
    string HelperString = string.Empty;

    bool IsLoadingData = false;

    async Task BarcodeScanned(BarcodeVM barcode)
    {
        if (BarcodeStore.Contains(barcode)) 
        {
            BarcodeStore.AddBarcode(barcode);
            HelperString = ALERT_BARCODE_SUCCESS;
            return;
        }
        if (FailedBarcodesCache.Contains(barcode.Barcode))
        {
            HelperString = string.Format(ALERT_BARCODE_NOT_FOUND, barcode.Barcode);
        }

        IsLoadingData = true;
        try
        {
            var barcodeData = await GetBarcodeData(barcode);
            if (barcodeData is null)
            {
                HelperString = string.Format(ALERT_BARCODE_NOT_FOUND, barcode.Barcode);
                FailedBarcodesCache.Add(barcode.Barcode);
            }
            else
            {
                if (Verifier is not null && !Verifier(barcodeData, out string reason))
                {
                    HelperString = reason;
                }
                else
                {
                    BarcodeStore.AddBarcode(barcodeData);
                    HelperString = ALERT_BARCODE_SUCCESS;
                }
            }
        }
        catch (Exception ex) 
        {
            HelperString = ex.Message;
        }
        IsLoadingData = false;
    }

    public void Close(bool keepChanges)
    {
        DialogService.Close(keepChanges);
    }

    Task<BarcodeVM?> GetBarcodeData(BarcodeVM barcode) => receivingHandler.GetBarcodeData(barcode.Barcode);

    public delegate bool BarcodeVerifier(BarcodeVM barcode, out string reason);
}
