using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;
using Domain.Entities.Enums.Transaction.Receiving;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseOrderLineDTO : ItemDTO
{
    public int LineNum { get; set; }
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public bool Free { get; set; }
    public decimal Price { get; set; } = 0;
    public decimal OpenQuantity { get; set; } = 0;
    public decimal TargetQuantity { get; set; } = 0;
    public WarehouseDTO Warehouse { get; set; }
    public string VatGroup { get; set; }
    public InputType InputType { get; set; } = InputType.Manual;
}