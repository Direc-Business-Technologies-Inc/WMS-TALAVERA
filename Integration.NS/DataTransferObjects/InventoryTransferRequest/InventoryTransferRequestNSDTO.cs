using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.InventoryTransferRequest;

public class InventoryTransferRequestNSDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SubsidiaryName { get; set; } = string.Empty;
    public int SubsidiaryId { get; set;  }
    public string SourceLocationName { get; set;} = string.Empty;
    public int SourceLocationId { get; set;}
    public string DestinationLocationName { get; set;} = string.Empty;
    public int DestinationLocationId { get; set;}
    public string PreparedBy { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string StatusId { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}
