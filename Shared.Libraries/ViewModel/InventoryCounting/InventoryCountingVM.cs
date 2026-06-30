using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.ViewModel.InventoryCounting;

public class InventoryCountingVM
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    
    public DateTime NetsuiteOrderCreatedDate { get; set; }

    public string GetName(string status) => status switch
    {
        "B" => "Started",
        _ => "Unknown"
    };

    public string GetShortName(string status) => status switch
    {
        "B" => "S",
        _ => "-"
    };
}
