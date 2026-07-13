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
    public string PurchaseUnitName { get; set; } = string.Empty;
    public string StockUnitName { get; set; } = string.Empty;
    public string SaleUnitName { get; set; } = string.Empty;
    public string UseBins
    {
        get => UsesBins ? "T" : "F";
        set => UsesBins = value == "T";
    }
    public int PurchaseUnitId { get; set; }
    public int StockUnitId { get; set; }
    public int SaleUnitId { get; set; }
    public decimal PurchaseUnitRate { get; set; }
    public decimal StockUnitRate { get; set; }
    public decimal SaleUnitRate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAvailable { get; set; }
    public bool UsesBins { get; set; }
}
