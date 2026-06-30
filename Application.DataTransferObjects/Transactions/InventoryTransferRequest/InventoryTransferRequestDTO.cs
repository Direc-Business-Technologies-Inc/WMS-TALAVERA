using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.InventoryTransferRequest;

public class InventoryTransferRequestDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public SubsidiaryDTO? Subsidiary { get; set; }
    public LocationDTO? SourceLocation { get; set; } 
    public LocationDTO? DestinationLocation { get; set; }
    public CustomerDTO? Customer { get; set; }
    public string PreparedBy { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<InventoryTransferRequestLineDTO> Lines { get; set; } = [];
}
