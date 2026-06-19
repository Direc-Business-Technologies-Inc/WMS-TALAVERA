using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.InventoryAdjustment;

public class InventoryAdjustmentLineNSDTO
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }
    public string UoMName { get; set; } = string.Empty;
    public decimal UoMRate { get; set; }
    public int UoMId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int LocationId { get; set; }
}
