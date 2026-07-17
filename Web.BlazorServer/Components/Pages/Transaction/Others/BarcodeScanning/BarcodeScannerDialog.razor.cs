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
        try
        {
            if (FailedBarcodesCache.Contains(barcode.Barcode))
                throw new Exception(string.Format(ALERT_BARCODE_NOT_FOUND, barcode.Barcode));

            BarcodeVM? barcodeData = BarcodeStore[barcode.Barcode] ?? await receivingHandler.GetBarcodeData(barcode.Barcode);

            if (barcodeData is null)
                throw new Exception(string.Format(ALERT_BARCODE_NOT_FOUND, barcode.Barcode));

            TryAddBarcode(barcodeData);
        }
        catch (Exception ex) 
        {
            HelperString = ex.Message;
        }
    }

    void TryAddBarcode(BarcodeVM barcode)
    {
        if (Verifier is not null && !Verifier(barcode, out string reason)) throw new Exception(reason);

        BarcodeStore.AddBarcode(barcode);
    }

    public void Close(bool keepChanges)
    {
        DialogService.Close(keepChanges);
    }

    Task<BarcodeVM?> GetBarcodeData(BarcodeVM barcode) => receivingHandler.GetBarcodeData(barcode.Barcode);

    public delegate bool BarcodeVerifier(BarcodeVM barcode, out string reason);
}
