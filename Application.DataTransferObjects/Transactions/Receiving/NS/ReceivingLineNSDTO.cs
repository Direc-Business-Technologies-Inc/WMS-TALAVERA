using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving.NS;
public class ReceivingLineNSDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoMId{ get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string UoMRate { get; set; } = string.Empty;
    public string LineType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;

    public decimal WeightReceived { get; set; }
    public decimal WeightTotal { get; set; }

    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime DocumentDate { get; set; }
}
