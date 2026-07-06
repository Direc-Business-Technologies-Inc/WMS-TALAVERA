using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.ViewModel.VendorReturnAuthorization;
public class VendorReturnAuthorizationVM
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
        "B" => "Pending Return",
        "E" => "Pending Credit/Partially Returned",
        _ => "Unknown"
    };

    public string GetShortName(string status) => status switch
    {
        "B" => "PR",
        "E" => "PC/PRR",
        _ => "-"
    };
}
