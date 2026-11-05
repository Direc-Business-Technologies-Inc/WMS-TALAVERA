namespace Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

public class VendorReturnAuthorizationPackingDataGridVM
{
    public int Id { get; set; }
    public int ForPrintingIFCount { get; set; }
    public int PrintedIFCount { get; set; }
    public int SingleFulfillmentId { get; set; }
    public int PendingPackLinesCount { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string SourceSubsidiary { get; set; } = string.Empty;
    public string DestinationSubsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}
