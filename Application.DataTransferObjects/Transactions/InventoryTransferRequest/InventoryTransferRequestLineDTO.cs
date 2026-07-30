using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.InventoryTransferRequest;

public class InventoryTransferRequestLineDTO
{
    public int? LineNumber { get; set; }
    public int? SourceLine { get; set; }
    public int ItemID { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set;  } = string.Empty;
    public ItemUnitDTO? UoM { get; set; }
    public LocationDTO? Location { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal Rate { get; set; }
    public decimal QuantityAlloted { get; set; }
    public bool IsAllAssigned => InventoryDetails.Sum(x => x.QuantityAlloted) == QuantityAlloted;
    public bool IsDirty { get; set; } = false;
    public bool ItemUsesBins { get; set; }
    public List<InventoryDetailDTO> InventoryDetails { get; set; } = [];
}
