namespace Application.DataTransferObjects.Transactions.Receiving.NS;

public class PurchaseOrderLineDTO
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;

    public int NetsuiteSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }

    public int NetsuiteLocationInternalId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationUsedBin { get; set; } = string.Empty;

    public int LineSequenceNumber { get; set; }
    public string TransactionLineType { get; set; } = string.Empty;

    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

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
}
