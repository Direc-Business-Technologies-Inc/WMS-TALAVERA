namespace Shared.Libraries.ViewModel;
public class ReturnsVM
{
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    public int  TransferCategory { get; set; }

    public DateTime NetsuiteOrderCreatedDate { get; set; }

    public string GetName(string status) => status switch
    {
        "F" => "Pending Receipt",
        "E" => "Partially Receipt/Pending Bill",
        _ => "Unknown"
    };

    public string GetShortName(string status) => status switch
    {
        "F" => "PR",
        "E" => "PR/PB",
        _ => "-"
    };

    public string GetTransferCategory(int category) => category switch
    {
        3 => "Good Items",
        4 => "Bad Items",
        _ => "-"
    };
}