using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects;

public class InventoryAdjustmentNSDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public int SubsidiaryId { get; set; }
    public string Location { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string Account { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
}
