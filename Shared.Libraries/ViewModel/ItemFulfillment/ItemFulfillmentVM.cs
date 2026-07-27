namespace Shared.Libraries.ViewModel.ItemFulfillment;

public class ItemFulfillmentVM
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; }
    public string DestinationLocation { get; set; } = string.Empty;
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    public DateTime NetsuiteOrderCreatedDate { get; set; }


    public bool isScanned { get; set; }


    public string GetName(string status) => status switch
    {
        "B" => "Packed",
        "C" => "Shipped",
        _ => "Unknown"
    };

    public string GetShortName(string status) => status switch
    {
        "B" => "P",
        "C" => "S",
        _ => "-"
    };
}
