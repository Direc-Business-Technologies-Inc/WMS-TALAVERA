namespace Application.DataTransferObjects.Transactions.Packing.VendorReturnAuthorization;

public class VendorReturnAuthorizationInfoDTO
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string FromSubsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
}
