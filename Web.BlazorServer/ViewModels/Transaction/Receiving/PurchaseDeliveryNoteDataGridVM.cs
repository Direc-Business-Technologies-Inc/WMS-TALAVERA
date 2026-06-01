namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class PurchaseDeliveryNoteDataGridVM
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int BaseEntry { get; set; }
    public int BaseDocNum { get; set; }
    public DateTime PoDocDate { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; }
    public string WhsCode { get; set; } = string.Empty;
    public string WhsName { get; set; }
    public string SupplierContactPerson { get; set; }
    public string ItemDesc { get; set; }
    public string ReceivedBy { get; set; }
}
