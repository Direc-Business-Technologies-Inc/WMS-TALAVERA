namespace Application.DataTransferObjects.Transactions.Commons.NS;

public class InventoryItemDTO
{
    public int NetsuiteMaterialInternalId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal MaterialWeight { get; set; }
}
