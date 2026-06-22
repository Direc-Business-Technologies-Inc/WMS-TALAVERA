namespace Application.DataTransferObjects.Others.NS;

public class OrdersDTO : TransactionDTO
{
    public int TransferCategory { get; set; }

    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }
    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

    public string LocationInternalId { get; set; } = string.Empty;
    public string LocationUsedBin { get; set; } = string.Empty;

    public int VendorEntityId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public string ParentCustomerEntityId { get; set; } = string.Empty;
    public int ParentCustomerInternalId { get; set; }
    public string CustomerEntityId { get; set; } = string.Empty;
    public int CustomerInternalId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public decimal OrderTotalAmount { get; set; }
    public int AssignedQuantity { get; set; }
}
