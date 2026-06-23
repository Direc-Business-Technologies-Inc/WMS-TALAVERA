using System.Numerics;
using static Integration.NS.Entities.NetSuiteFindIdsResponse;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class TransferOrderLineVM
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;

    public decimal QuantityPlanned { get; set; }
}

