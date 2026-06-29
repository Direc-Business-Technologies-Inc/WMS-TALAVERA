namespace Integration.NS.DataTransferObjects.Packing.STR;

public class StrPackingDataGridNSDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string StatusId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
