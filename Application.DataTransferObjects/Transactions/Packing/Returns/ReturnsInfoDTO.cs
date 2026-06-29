namespace Application.DataTransferObjects.Transactions.Packing.Returns;

public class ReturnsInfoDTO
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string FromSubsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
}
