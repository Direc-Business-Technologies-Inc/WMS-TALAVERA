using Application.DataTransferObjects.Others;

namespace Application.DataTransferObjects.Transactions.Packing.STR;

public class StockTransferRequestInfoPackingDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public VendorDTO? Vendor { get; set; } = null;
    public LocationDTO? SourceLocation { get; set; } = null;
    public LocationDTO? DestinationLocation { get; set; } = null;
    public SubsidiaryDTO? Subsidiary { get; set; } = null;
    public SubsidiaryDTO? ToSubsidiary { get; set; } = null;
    public TransferOrderStatusPacking? Status { get; set; } = null;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public TransferCategoryPacking TransferCategory { get; set; } = TransferCategoryPacking.Transfer;
    public DateTime Date { get; set; }
    public List<StockTransferRequestLinePackingDTO> Lines { get; set; } = [];
}
