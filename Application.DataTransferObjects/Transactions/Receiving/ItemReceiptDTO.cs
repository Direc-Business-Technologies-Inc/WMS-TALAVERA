using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class ItemReceiptDTO
{
    public SourceTypes SourceType { get; set; } = SourceTypes.PurchaseOrder;
    public int SourceInternalId { get; set; }
    public int VendorPrefferedBin { get; set; }
    public int DefaultBO { get; set; }

    public string Type { get; set; } = string.Empty;
    public string CreatedFrom { get; set; } = string.Empty;
    public string Department { get; set; } = "Operations";
    public string Vendor { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Now;

    public List<ItemReceiptLineDTO> Lines = [];

    public enum SourceTypes
    {
        TransferOrder,
        PurchaseOrder,
        Returns
    }
}

public class ItemReceiptLineDTO
{
    public int LineNumber { get; set; }
    public int PrefferedBinAssignmentId { get; set; }

    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Department { get; set; } = "Operations";
    public string Location { get; set; } = string.Empty;
    public string LocationUsesBins 
    {
        get => _isLocationBinUsed ? "T" : "F";
        set => _isLocationBinUsed = value.Equals("T", StringComparison.OrdinalIgnoreCase);
    }

    public decimal WeightReceived { get; set; }
    public decimal WeightTotal { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }
    public decimal QuantityGood { get; set; }

    public bool IsLocationBinUsed {
        get => _isLocationBinUsed;
        set => _isLocationBinUsed = value;
    }

    public bool _isLocationBinUsed = false;
    public bool IsReceived { get; set; } = true;

}
