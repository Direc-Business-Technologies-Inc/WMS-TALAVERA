using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.Others;

public class ItemsNSDTO
{
    public int Id { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitTypeName { get; set; } = string.Empty;
    public int UnitTypeId { get; set; }
    public string PurchaseUnit { get; set; } = string.Empty;
    public string StockUnit { get; set; } = string.Empty;
    public string SaleUnit { get; set; } = string.Empty;
    public int PurchaseUnitId { get; set; }
    public int StockUnitId { get; set; }
    public int SaleUnitId { get; set; }
}
