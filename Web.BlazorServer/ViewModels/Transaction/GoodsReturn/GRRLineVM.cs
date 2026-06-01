using Application.DataTransferObjects.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

public class GRRLineVM : ItemVM
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public string WhsCode { get; set; }
    public string WhsName { get; set; }
    public decimal TargetQty { get; set; }
    public decimal OpenQty { get; set; }
    public WarehouseDTO Warehouse { get; set; }
}
