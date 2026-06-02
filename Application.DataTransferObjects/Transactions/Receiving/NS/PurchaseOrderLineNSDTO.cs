using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving.NS;
public class PurchaseOrderLineNSDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal WeightReceived { get; set; }
    public decimal WeightTotal { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }
}
