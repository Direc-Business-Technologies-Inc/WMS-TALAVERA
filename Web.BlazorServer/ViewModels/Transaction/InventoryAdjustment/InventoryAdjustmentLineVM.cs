using Application.DataTransferObjects.Transactions.Receiving;
using Web.BlazorServer.ViewModels.Others;
using static Integration.NS.Entities.NetSuiteFindIdsResponse;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

public class InventoryAdjustmentLineVM
{
    //    Item Code
    //Item Description
    //UoM
    //Warehouse
    //On - hand Qty
    //Allotted Qty

    public int ItemId { get; set; }
    public int? LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set;  }
    public decimal QuantityOnHandByUoM => QuantityOnHand / (UoM?.ConversionRate ?? 1);
    public decimal QuantityAlloted { get; set;  }
    public ItemUnitVM? UoM { get; set; } = null;
    public LocationVM? Location { get; set; } = null;
    public List<InventoryDetailVM> InventoryDetails { get; set; } = [];

    public bool UsesBins { get; set; } = true;

    public decimal? QuantityAllotedMax => Type == Types.Receipt ? null : QuantityOnHand;
    public decimal QuantityAssignedToBins => InventoryDetails.Sum(x => x.QuantityAlloted);
    public decimal QuantityNew => Type == Types.Issue ? QuantityOnHandByUoM - QuantityAlloted : QuantityOnHandByUoM + QuantityAlloted;
    public decimal QuantityOld => Type == Types.Issue ? QuantityOnHandByUoM + QuantityAlloted : QuantityOnHandByUoM - QuantityAlloted;
    public bool IsAllAssignedToBins => !UsesBins || QuantityAssignedToBins == QuantityAlloted;

    public Types Type { get; set; } = Types.Receipt;
    public enum Types
    {
        Issue,
        Receipt
    }
}
