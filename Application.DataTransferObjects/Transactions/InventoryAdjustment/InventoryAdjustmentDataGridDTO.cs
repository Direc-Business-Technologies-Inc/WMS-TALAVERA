using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.InventoryAdjustment;

public class InventoryAdjustmentDataGridDTO
{
    public int Id { get; set; }
    public string Subsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
}
