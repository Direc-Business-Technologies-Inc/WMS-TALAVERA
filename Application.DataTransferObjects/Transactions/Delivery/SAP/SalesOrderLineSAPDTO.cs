using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.Delivery.SAP;

public class SalesOrderLineSAPDTO : ItemDTO
{
    public string WhsCode { get; set; }
    public string WhsName { get; set; }
    public decimal TargetQty { get; set; }
    public decimal OpenQty { get; set; }

}
