using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public class StockTransferRequestLineDTO
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }
}
