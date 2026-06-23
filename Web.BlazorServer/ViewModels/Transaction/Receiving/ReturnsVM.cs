using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ReturnsVM
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string FromSubsidiary { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string FromWarehouse { get; set; } = string.Empty;
    public string ToWarehouse { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public List<ReturnsLineVM> Lines { get; set; } = [];
}

