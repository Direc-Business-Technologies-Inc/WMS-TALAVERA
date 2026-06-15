namespace Shared.Libraries.ViewModel;

public class PackingVM
{
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    public int TransferCategory { get; set; }

    public DateTime NetsuiteOrderCreatedDate { get; set; }

    public string GetName(string status) => status switch
    {
        "B" => "Pending Fulfillment",
        "D" => "Partially Fulfilled",
        _ => "Unknown"
    };

    public string GetShortName(string status) => status switch
    {
        "B" => "PF",
        "D" => "PRF",
        _ => "-"
    };

    public string GetTransferCategory(int category) => category switch
    {
        3 => "Good Items",
        4 => "Bad Items",
        _ => "-"
    };
}
