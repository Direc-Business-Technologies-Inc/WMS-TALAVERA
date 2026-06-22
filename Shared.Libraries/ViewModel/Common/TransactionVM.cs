using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.ViewModel;

public class TransactionVM
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;

    public int NetsuiteSubsidiaryInternalId { get; set; }

    public int NetsuiteLocationInternalId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationUsedBin { get; set; } = string.Empty;
    public bool IsLocationUsedBin => LocationUsedBin == "T";

    public int LineSequenceNumber { get; set; }
    public string TransactionLineType { get; set; } = string.Empty;

    public int NetsuiteMaterialInternalId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal MaterialWeight { get; set; }

    public decimal LineQuantity { get; set; }
    public decimal LineQuantityReceived { get; set; }
    public decimal LineQuantityPacked { get; set; }
    public decimal LineQuantityShipped { get; set; }

    public int NetsuiteUoMInternalId { get; set; }
    public string UoMName { get; set; } = string.Empty;
    public decimal UoMRate { get; set; }

    public DateTime NetsuiteOrderCreatedDate { get; set; }
    public DateTime NetsuiteOrderDocumentDate { get; set; }
    public DateTime NetsuiteOrderUpdatedDate { get; set; }


    public decimal? DefaultWeight;

    public decimal TotalWeight => LineQuantity * MaterialWeight; // Record Weight

    public decimal NSLineQuantityReceived => (LineQuantity - LineQuantityReceived) / UoMRate;
    public decimal NSLineQuantityPacked => (LineQuantity - LineQuantityPacked) / UoMRate;
    public decimal NSLineQuantityShipped => (LineQuantity - LineQuantityShipped) / UoMRate;

    // For Scanning
    public int ScanCount { get; set; }

    // Keep existing total scanned quantity for backward compat
    public decimal ScannedQuantity { get; set; }
    public decimal ScannedWeight { get; set; }

    // New: classification flag - true = Bad, false = Good (default)
    public bool IsBad { get; set; } = false;

    public bool AlreadyReceived => ScannedQuantity == NSLineQuantityReceived;
    public bool AlreadyPacked => ScannedQuantity == NSLineQuantityPacked;
    public bool AlreadyShipped => ScannedQuantity == NSLineQuantityShipped;
}
