namespace Web.BlazorServer.ViewModels.Transaction.Packing.ItemReceipt;

public class ItemReceiptLinePackingVM
{
    private bool _isReceived = true;

    public bool IsReceived
    {
        get => _isReceived && !IsComplete;
        set => _isReceived = value;
    }

    public bool IsComplete => QuantityOpen <= 0;
    public bool IsLocationBinUsed { get; set; } = false;

    public int LineNumber { get; set; }
    public int PrefferedBinAssignmentId { get; set; }
    public int VendorAssignedBinId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public decimal QuantityAvailable { get; set; }

    public decimal QuantityPlanned { get; set; }
    public decimal QuantityBackOrdered { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }
    public decimal QuantityGood { get; set; }
}
