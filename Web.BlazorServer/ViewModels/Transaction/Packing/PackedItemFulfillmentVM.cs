using Web.BlazorServer.Components.Custom.Utilities;

namespace Web.BlazorServer.ViewModels.Transaction.Packing;

public class PackedItemFulfillmentVM
{
    [QuickDataGridIgnore]
    public int Id { get; set; }
    [QuickDataGridTitle("Item Fulfillment Number")]
    public string ReferenceNumber { get; set; } = string.Empty;
    [QuickDataGridTitle("Created From")]
    public string CreatedFrom { get; set; } = string.Empty;
    [QuickDataGridTitle("Transfer Category")]
    public string TransferCategory { get; set; } = string.Empty;
}
