using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class BarcodeDTO
{
    public string Barcode { get; set; } = string.Empty;
    public ItemsDTO? Item { get; set; }
    public ItemUnitDTO? UoM { get; set; }
}
