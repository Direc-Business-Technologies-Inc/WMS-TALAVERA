using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;
using Domain.Entities.Enums.Transaction.Receiving;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseDeliveryNoteLineDTO : ItemDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int BaseEntry { get; set; }
    public int BaseDocNum { get; set; }
    public int LineNum { get; set; }
    public int BaseLine { get; set; }
    public decimal Price { get; set; }
    public string TaxCode { get; set; }
    public bool Free { get; set; }
    public decimal OpenQty { get; set; }
    public BusinessPartnerDTO BusinessPartner { get; set; }
    public WarehouseDTO Warehouse { get; set; }
    public InputType InputType { get; set; }
}