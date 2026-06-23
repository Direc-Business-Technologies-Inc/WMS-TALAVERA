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
    public SubsidiaryDTO? Subsidiary { get; set; }
    public LocationDTO? Location { get; set; }
    public BusinessAccountDTO? Account { get; set; }
    public InventoryAdjustmentReasonDTO? Reason { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public List<InventoryAdjustmentLineDTO> Lines { get; set; } = [];
}
