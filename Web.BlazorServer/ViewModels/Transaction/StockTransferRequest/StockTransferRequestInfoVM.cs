namespace Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

public class StockTransferRequestInfoVM
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Requestor { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<StockTransferRequestLineVM> Lines { get; set; } = [];
}
