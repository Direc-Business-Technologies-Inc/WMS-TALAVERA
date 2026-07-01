using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.InventoryTransferRequest;

public class InventoryTransferRequestDataGridDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SubsidiaryName { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
