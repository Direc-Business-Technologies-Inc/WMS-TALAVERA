using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public class SupplierReturnLineDTO
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public ItemUnitDTO? UoM { get; set; }
    public LocationDTO? Location { get; set; }
    public decimal QuantityAlloted { get; set; }
    public decimal QuantityAvailable { get; set; }
    public int? LineNumber { get; set; }
}
