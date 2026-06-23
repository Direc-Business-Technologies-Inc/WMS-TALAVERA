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
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set;  }
    public decimal QuantityAlloted { get; set;  }
    public ItemUnitVM? UoM { get; set; } = null;
    public LocationVM? Location { get; set; } = null;
    public List<InventoryDetailVM> InventoryDetails { get; set; } = [];

    public decimal QuantityAssignedToBins => InventoryDetails.Sum(x => x.QuantityAlloted);
    public bool IsAllAssignedToBins => QuantityAssignedToBins == QuantityAlloted;

    public Types Type { get; set; } = Types.Receipt;
    public enum Types
    {
        Issue,
        Receipt
    }
}
