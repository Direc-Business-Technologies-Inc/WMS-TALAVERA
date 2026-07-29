using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.InventoryAdjustment;

public class InventoryAdjustmentLineDTO
{
    public int ItemId { get; set; }
    public int? LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }
    public ItemUnitDTO UoM { get; set; } = new();
    public LocationDTO Location { get; set; } = new();
    public List<InventoryDetailDTO> InventoryDetails { get; set; } = [];
}
