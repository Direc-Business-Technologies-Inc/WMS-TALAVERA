namespace Integration.SAP.Entities.Transactional.Receiving;

public class PurchaseOrderSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string SupplierContactPerson { get; set; }
    public string Remarks { get; set; }
    public string DocStatus { get; set; }
    public int Time { get; set; }
}
