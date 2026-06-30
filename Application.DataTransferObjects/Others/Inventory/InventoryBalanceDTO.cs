using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others.Inventory;

public class InventoryBalanceDTO
{
    public int ItemId { get; set; }
    public LocationBinDTO? Bin { get; set; }
    public LocationDTO? Location { get; set; }
    public InventoryStatusDTO? Status { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityCommited { get; set; }
}
