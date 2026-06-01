namespace Application.DataTransferObjects.Transactions.Delivery.SAP;

public class DeliveryHeaderSAPDTO
{
    public int DocNum { get; set; }
    public int DocEntry { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string ContactPerson { get; set; }
    public string NumAtCard { get; set; }

    public string? SchoolYear { get; set; }
    public string? DRNo { get; set; }
    public string? ActualDeliveryDate { get; set; }
    public string? DeliveryMeans { get; set; }
    public string? Courier { get; set; }
    public string? CourierName { get; set; }
    public string? Designation { get; set; }
    public string? WaybillNo { get; set; }
    public string? PlateNo { get; set; }
    public string? Driver { get; set; }
    public string? DocRemarks { get; set; }
    public string? ReceivedBy { get; set; }
    public string? PreparedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? NotedBy { get; set; }
}
