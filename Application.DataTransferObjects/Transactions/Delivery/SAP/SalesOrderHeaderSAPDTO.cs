namespace Application.DataTransferObjects.Transactions.Delivery.SAP;

public class SalesOrderHeaderSAPDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string ContactPerson { get; set; }
    public string NumAtCard { get; set; }

    public string? SchoolYear { get; set; }
    public string? PONo { get; set; }
    public string? Area { get; set; }
    public string? Designation { get; set; }
    public string? OrderBy { get; set; }
    public string? DocRemarks { get; set; }
    public string? PreparedBy { get; set; }
    public string? ReviewedBy { get; set; }
    public string? AppprovedBy { get; set; }
    public string? NotedBy { get; set; }
}
