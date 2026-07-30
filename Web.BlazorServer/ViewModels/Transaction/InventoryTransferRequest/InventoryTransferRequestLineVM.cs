using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

public class InventoryTransferRequestLineVM
{
    public int ItemID { get; set; }
    public int? LineNumber { get; set; }
    public int? SourceLine { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public ItemUnitVM? UoM { get; set; }
    public LocationVM? Location { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityOnHandByUoM => QuantityOnHand / (UoM?.ConversionRate ?? 1);
    public decimal QuantityAlloted { get; set; }
    public bool UsesBins { get; set; }
    public bool ItemUsesBins { get; set; }
    public bool IsAllAssigned => !ItemUsesBins || LineNumber is not null || InventoryDetails.Sum(x => x.QuantityAlloted) == QuantityAlloted;
    public List<InventoryDetailVM> InventoryDetails { get; set; } = [];
}
