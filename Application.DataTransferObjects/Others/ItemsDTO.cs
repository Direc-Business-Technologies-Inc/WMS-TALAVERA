using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others;

public class ItemsDTO
{
    public int Id { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitTypeName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public int UnitTypeId { get; set; }
    public ItemUnitDTO PurchaseUnit { get; set; } = new(); 
    public ItemUnitDTO StockUnit { get; set; } = new(); 
    public ItemUnitDTO SaleUnit { get; set; } = new(); 
    public bool UsesBins { get; set; }
}
