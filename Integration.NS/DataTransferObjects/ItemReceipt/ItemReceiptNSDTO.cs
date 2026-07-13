using Application.DataTransferObjects.Transactions.Receiving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.ItemReceipt;

public class ItemReceiptNSDTO
{

    public SourceTypes SourceType { get; set; } = SourceTypes.PurchaseOrder;
    public int SourceInternalId { get; set; }
    public int VendorPrefferedBin { get; set; }
    public int DefaultBO { get; set; }
    public int? PreparedById { get; set; }

    public string Type { get; set; } = string.Empty;
    public string CreatedFrom { get; set; } = string.Empty;
    public string Department { get; set; } = "Operations";
    public string Vendor { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string TransferLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;

    public enum SourceTypes
    {
        TransferOrder,
        PurchaseOrder,
        Returns
    }
}
