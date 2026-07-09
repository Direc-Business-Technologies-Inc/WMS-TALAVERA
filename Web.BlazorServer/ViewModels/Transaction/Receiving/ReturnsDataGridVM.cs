using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ReturnsDataGridVM
{
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
