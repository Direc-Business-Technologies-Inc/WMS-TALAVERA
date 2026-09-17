using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.ItemFulfillment;

public class ItemFulfillmentDTO
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; }
    public string DestinationLocation { get; set; } = string.Empty;
    public int NetsuiteToLocationInternalId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    public DateTime NetsuiteOrderCreatedDate { get; set; }
}
