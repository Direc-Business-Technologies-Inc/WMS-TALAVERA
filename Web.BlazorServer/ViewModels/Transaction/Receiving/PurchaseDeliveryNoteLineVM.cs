using Domain.Entities.Enums.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class PurchaseDeliveryNoteLineVM : ItemVM
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int BaseEntry { get; set; }
    public int BaseDocNum { get; set; }
    public int BaseLine { get; set; }
    public string TaxCode { get; set; }
    public bool Free { get; set; }
    public decimal Price { get; set; }
    public decimal OpenQty { get; set; }
    public WarehouseVM Warehouse { get; set; } = new();
    public InputType InputType { get; set; } = InputType.Manual;
}
