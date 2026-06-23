using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others;

public class InventoryDetailDTO
{
    public LocationBinDTO? Bin { get; set; } = null;
    public decimal QuantityAlloted { get; set; }
}
