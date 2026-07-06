namespace Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

public class ReturnsItemReceiptLinePackingVM
{
    private bool _isReceived = true;

    public bool IsReceived
    {
        get => _isReceived && QuantityReceived < QuantityPlanned;
        set => _isReceived = value;
    }

    public bool IsComplete => QuantityPlanned <= QuantityReceived;
    public bool IsLocationBinUsed { get; set; } = false;

    public int LineNumber { get; set; }
    public int PrefferedBinAssignmentId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityBackOrdered { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }
    public decimal QuantityGood { get; set; }
}
