using Domain.Entities.Enums.Transaction.Commons;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseOrderDataGridDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    public DocumentStatus DocStatus { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string SupplierContactPerson { get; set; }
    public string Remarks { get; set; }
}
