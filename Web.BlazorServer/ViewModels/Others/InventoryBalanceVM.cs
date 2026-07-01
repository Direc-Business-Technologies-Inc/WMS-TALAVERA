using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.Inventory;

namespace Web.BlazorServer.ViewModels.Others;

public class InventoryBalanceVM
{
    public int ItemId { get; set; }
    public LocationBinVM? Bin { get; set; }
    public LocationVM? Location { get; set; }
    public InventoryStatusVM? Status { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityCommited { get; set; }
    public decimal QuantityAvailable => QuantityOnHand - QuantityCommited;
    public string StatusName => Status?.Name ?? "NONE";
}
