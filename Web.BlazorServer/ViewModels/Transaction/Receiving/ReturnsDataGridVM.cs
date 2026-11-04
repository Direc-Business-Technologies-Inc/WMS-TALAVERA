using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Web.BlazorServer.Components.Custom.Utilities;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ReturnsDataGridVM
{
    [QuickDataGridIgnore]
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string FromSubsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string SourceWarehouse { get; set; } = string.Empty;
    public string DestinationWarehouse { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
