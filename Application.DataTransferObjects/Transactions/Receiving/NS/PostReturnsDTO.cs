using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving.NS;

public class PostReturnsDTO
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public int TransferCategory { get; set; }

    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

    public string LocationName { get; set; } = string.Empty;
    public string LocationUsedBin { get; set; } = string.Empty;
    public bool IsLocationUsedBin { get; set; }

    public int LineSequenceNumber { get; set; }
    public string TransactionLineType { get; set; } = string.Empty;

    public int NetsuiteMaterialInternalId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public decimal MaterialWeight { get; set; }

    public decimal LineQuantity { get; set; }
    public decimal LineQuantityReceived { get; set; }
    public int NetsuiteUoMInternalId { get; set; }
    public string UoMName { get; set; } = string.Empty;
    public decimal UoMRate { get; set; }

    public DateTime NetsuiteOrderCreatedDate { get; set; }
    public DateTime NetsuiteOrderDocumentDate { get; set; }
    public DateTime NetsuiteOrderUpdatedDate { get; set; }


    public decimal TotalWeight { get; set; }

    public decimal NSLineQuantity { get; set; }
    public decimal NSLineQuantityReceived { get; set; }

    public int ScanCount { get; set; }

    public decimal ScannedQuantity { get; set; }
    public decimal ScannedWeight { get; set; }
    public decimal TotalQuantity { get; set; }

    public bool IsBad { get; set; }

    public bool AlreadyFulfilled { get; set; }
    public bool OverScanned { get; set; }
}
