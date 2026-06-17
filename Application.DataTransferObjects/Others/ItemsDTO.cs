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

    public string PurchaseUnit { get; set;  } = string.Empty;
    public int PurchaseUnitId { get; set; }

    public string StockUnit { get; set;  } = string.Empty;
    public int StockUnitId { get; set; }

    public string SaleUnit { get; set;  } = string.Empty;
    public int SaleUnitId { get; set; }

    public decimal QuantityOnHand { get; set; }
}
