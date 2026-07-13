using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.ItemReceipt;

public class ItemReceiptLinesNSDTO
{

    public int LineNumber { get; set; }
    public int PrefferedBinAssignmentId { get; set; }

    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Department { get; set; } = "Operations";
    public string LocationName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationUsesBins
    {
        get => _isLocationBinUsed ? "T" : "F";
        set => _isLocationBinUsed = value.Equals("T", StringComparison.OrdinalIgnoreCase);
    }

    public decimal UoMRate { get; set; }
    public decimal WeightActual { get; set; }
    public decimal WeightPerItem { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAlloted { get; set; }

    public bool IsLocationBinUsed
    {
        get => _isLocationBinUsed;
        set => _isLocationBinUsed = value;
    }

    public bool _isLocationBinUsed = false;
    public bool IsReceived { get; set; } = true;
}
