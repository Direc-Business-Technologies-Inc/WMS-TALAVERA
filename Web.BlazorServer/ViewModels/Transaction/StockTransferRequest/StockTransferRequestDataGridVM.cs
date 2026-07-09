namespace Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

public class StockTransferRequestDataGridVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string StatusDisplayString => Status.Name;
    public bool IsEditable { get; set; }
    public TransferOrderStatusVM Status { get; set; } = new();
    public DateTime Date { get; set; }
}
