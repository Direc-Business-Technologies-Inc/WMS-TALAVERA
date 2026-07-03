using Application.DataTransferObjects.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class BarcodeVM
{
    public string Barcode { get; set; } = string.Empty;
    public ItemsVM? Item { get; set; }
    public ItemUnitVM? UoM { get; set; }
}
