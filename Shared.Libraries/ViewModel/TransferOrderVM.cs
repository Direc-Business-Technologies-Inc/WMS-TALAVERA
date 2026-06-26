using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.ViewModel;

public class TransferOrderVM
{
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
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
}
