using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.ViewModel.Common;

public class InventoryItemVM
{
    public int NetsuiteMaterialInternalId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal MaterialWeight { get; set; }
}
