namespace Shared.Libraries.ViewModel.PurchaseOrder;

public class PurchaseOrderVM
{
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    public string CustomerEntityId { get; set; }
    public string CustomerName { get; set; }
    public DateTime NetsuiteOrderDocumentDate { get; set; }
    public DateTime NetsuiteOrderCreatedDate { get; set; }

    public string GetName(string status) => status switch
    {
        "B" => "Pending Receipt",
        "E" => "Partially Receipt/Pending Bill",
        _ => "Unknown"
    };

    public string GetShortName(string status) => status switch
    {
        "B" => "PR",
        "E" => "PR/PB",
        _ => "-"
    };
}
