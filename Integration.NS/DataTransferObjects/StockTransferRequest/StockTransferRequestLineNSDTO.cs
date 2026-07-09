using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.StockTransferRequest;

public class StockTransferRequestLineNSDTO
{
    public int? LineNumber { get; set; }
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoMName { get; set; } = string.Empty;
    public int UoMId { get; set; }
    public decimal UoMRate { get; set; }
    public string Warehouse { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }
}
