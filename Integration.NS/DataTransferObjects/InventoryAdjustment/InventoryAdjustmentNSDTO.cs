using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.InventoryAdjustment;

public class InventoryAdjustmentNSDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SubsidiaryName { get; set; } = string.Empty;
    public int SubsidiaryId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
}
