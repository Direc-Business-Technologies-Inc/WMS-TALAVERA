using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.InventoryAdjustment;

public class InventoryAdjustmentDTO
{
    public int Id { get; set; }
    public int PreparedById { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public SubsidiaryDTO? Subsidiary { get; set; }
    public LocationDTO? Location { get; set; }
    public BusinessAccountDTO? Account { get; set; }
    public InventoryAdjustmentReasonDTO? Reason { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<InventoryAdjustmentLineDTO> Lines { get; set; } = [];
}
