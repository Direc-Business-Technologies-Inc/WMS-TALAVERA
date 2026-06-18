using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.InventoryAdjustment;

public class InventoryAdjustmentLineDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }
}
