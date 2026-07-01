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

    public int LocationId {
        get => Location?.Id ?? -1;
        set
        {
            if (Location != null) Location.Id = value;
        }
    }
    public string LocationName
    {
        get => Location?.Name ?? string.Empty;
        set
        {
            if (Location != null) Location.Name = value;
        }
    }

    public int StatusId {
        get => Status?.Id ?? -1;
        set
        {
            if (Status != null) Status.Id = value;
        }
    }
    public string StatusName
    {
        get => Status?.Name ?? string.Empty;
        set
        {
            if (Status != null) Status.Name = value;
        }
    }

    public int BinId {
        get => Bin?.Id ?? -1;
        set
        {
            if (Bin != null) Bin.Id = value;
        }
    }
    public string BinName
    {
        get => Bin?.BinNumber ?? string.Empty;
        set
        {
            if (Bin != null) Bin.BinNumber = value;
        }
    }
}
