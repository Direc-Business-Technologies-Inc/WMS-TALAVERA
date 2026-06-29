using Application.DataTransferObjects.Transactions.Packing.STR;

namespace Integration.NS.DataTransferObjects.Packing.STR;

public class StrPackingHeaderNSDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int VendorId { get; set; } 
    public string SourceLocationName { get; set; } = string.Empty;
    public int SourceLocationId { get; set; }
    public string DestinationLocationName { get; set; } = string.Empty;
    public int DestinationLocationId { get; set; }
    public string SubsidiaryName { get; set; } = string.Empty;
    public int SubsidiaryId { get; set; }
    public string ToSubsidiaryName { get; set; } = string.Empty;
    public int ToSubsidiaryId { get; set; }
    public int TransferCategoryId { get; set; }
    public string TransferCategoryName { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty; 
    public string StatusId { get; set; } = string.Empty; 
    public DateTime Date { get; set; }
    public List<StockTransferRequestLinePackingDTO> Lines { get; set; } = [];
}