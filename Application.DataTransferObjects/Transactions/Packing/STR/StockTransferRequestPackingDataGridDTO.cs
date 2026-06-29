namespace Application.DataTransferObjects.Transactions.Packing.STR;

public class StockTransferRequestPackingDataGridDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public TransferOrderStatusPacking? Status { get; set; } = null;
    public DateTime Date { get; set; }
}
