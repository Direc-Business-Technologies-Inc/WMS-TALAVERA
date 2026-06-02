using Application.DataTransferObjects.Others;
using Domain.Entities.Enums.Transaction.Receiving;
using System.Numerics;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using static Integration.NS.Entities.NetSuiteFindIdsResponse;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class PurchaseOrderLineVM
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal WeightReceived { get; set; }
    public decimal WeightTotal { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }

}
