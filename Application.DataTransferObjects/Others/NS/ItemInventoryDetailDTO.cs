namespace Application.DataTransferObjects.Others.NS;

public class ItemInventoryDetailDTO
{
	public int NetsuiteMaterialInternalId { get; set; }
	public int MaterialAvailableQuantity { get; set; }
	public int NetsuiteBinInternalId { get; set; }
	public int NetsuiteLocationInternalId { get; set; }
	public int NetsuiteInventoryStatusInternalId { get; set; }
}
