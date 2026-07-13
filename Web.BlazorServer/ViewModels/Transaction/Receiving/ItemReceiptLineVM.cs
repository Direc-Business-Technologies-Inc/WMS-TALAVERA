using System.Numerics;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ItemReceiptLineVM
{
    private bool _isReceived = true;
    public bool IsReceived
    {
        get
        {
            return _isReceived && (QuantityReceived < QuantityPlanned);
        }
        set
        {
            _isReceived = value;
        }
    }
    public bool IsComplete => QuantityPlanned <= QuantityReceived;
    public bool IsLocationBinUsed { get; set; } = false;

    public int LineNumber { get; set; }
    public int PrefferedBinAssignmentId { get; set; }

    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } =  string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public decimal UoMRate { get; set; }
    public decimal WeightActual { get; set; }
    public decimal WeightPerItem { get; set; }
    public decimal WeightRecord => WeightPerItem * QuantityAlloted;
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAlloted { get; set; }
    public bool IsAllAssigned => InventoryDetails.Sum(x => x.QuantityAlloted) == QuantityAlloted;
    public List<InventoryDetailVM> InventoryDetails { get; set; } = [];

}
