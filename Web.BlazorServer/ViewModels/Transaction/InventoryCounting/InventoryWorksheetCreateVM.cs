using SharedWorksheetLineVM = Shared.Libraries.ViewModel.InventoryCounting.InventoryWorksheetLineVM;
using SharedInventoryItemVM = Shared.Libraries.ViewModel.Common.InventoryItemVM;
using SharedLocationVM = Shared.Libraries.ViewModel.LocationVM;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

public class InventoryWorksheetCreateVM
{
    public SharedLocationVM? Location { get; set; }
    public List<InventoryWorksheetCreateLineVM> Lines { get; set; } = [];
}

public class InventoryWorksheetCreateLineVM : SharedWorksheetLineVM
{
    public SharedInventoryItemVM? SelectedItem { get; set; }
    public decimal TotalQuantity { get; set; }
    public List<InventoryWorksheetDetailLineVM> Details { get; set; } = [];
    public decimal AllocatedQuantity => Details.Sum(line => line.Quantity);
}

public class InventoryWorksheetDetailLineVM : SharedWorksheetLineVM
{
    public LocationBinVM? Bin { get; set; }
    public int NetsuiteBinInternalId { get; set; }
    public InventoryWorksheetDetailStatus Status { get; set; } = InventoryWorksheetDetailStatus.Good;
    public decimal Quantity { get; set; }
}

public enum InventoryWorksheetDetailStatus
{
    Good = 1,
    Bad = 3
}
