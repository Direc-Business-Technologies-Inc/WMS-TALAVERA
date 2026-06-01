using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.SalesReturn;

public class SalesReturnDTO : MarketingDocumentDTO
{
    public string? ContactPerson { get; set; }
    public string? NumAtCard { get; set; }
    public string? SchoolYear { get; set; }
    public string? ReturnType { get; set; }
    public string? PURNo { get; set; }
    public string? DRNo { get; set; }
    public string? SONo { get; set; }
    public string? SINo { get; set; }
    public string? Designation { get; set; }
    public string? DocRemarks { get; set; }
    public string? ReturnedBy { get; set; }
    public string? PickBy { get; set; }
    public string PreparedBy { get; set; } = string.Empty;
    public string? CheckedBy { get; set; }
    public string? NotedBy { get; set; }
    public string? ApprovedBy { get; set; }

    public int DeliveryDocEntry { get; set; }
    public int DeliveryDocNum { get; set; }
    public int SalesReturnRequestDocEntry { get; set; }
    public int SalesReturnRequestDocNum { get; set; }

    public List<SalesReturnLineDTO> DocumentLines { get; set; } = [];
}
