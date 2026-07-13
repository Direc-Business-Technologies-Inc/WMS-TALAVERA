using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.SupplierReturn;

public class SupplierReturnLineNSDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string UoMName { get; set; } = string.Empty;
    public int UoMId { get; set; }
    public decimal UoMRate { get; set; }
    public string LocationName { get; set;} = string.Empty;
    public int LocationId { get; set; } 
    public decimal QuantityAlloted { get; set; }
    public decimal QuantityAvailable { get; set; }
    public int? LineNumber { get; set; }
}
