namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public class BulkFulfillmentStatusDTO
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? Memo { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
