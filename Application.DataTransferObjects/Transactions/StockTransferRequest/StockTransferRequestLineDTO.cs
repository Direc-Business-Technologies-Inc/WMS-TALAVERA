using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public class StockTransferRequestLineDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityAlloted { get; set; }
}
