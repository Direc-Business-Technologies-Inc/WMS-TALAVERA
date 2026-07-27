using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.ViewModel.TransferOrder;

public class TransferOrderVM
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    public DateTime NetsuiteOrderCreatedDate { get; set; }

    public string GetName(string status) => status switch
    {
        "F" => "Pending Receipt",
        "E" => "Pending Receipt/Partially Fulfilled",

        "B" => "Pending Fulfillment",
        "D" => "Partially Fulfilled",
        _ => "Unknown"
    };

    public string GetShortName(string status) => status switch
    {
        "F" => "PR",
        "E" => "PR/PRF",

        "B" => "PF",
        "D" => "PRF",
        _ => "-"
    };
}
