using Application.DataTransferObjects.Others.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others;

public class InventoryDetailDTO
{
    public int? Id { get; set; }
    public LocationBinDTO? Bin { get; set; } = null;
    public InventoryStatusDTO? Status { get; set; }
    public decimal QuantityAlloted { get; set; }
}
