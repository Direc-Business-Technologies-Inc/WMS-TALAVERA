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

    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set;  }
    public decimal QuantityAlloted { get; set;  }
}
