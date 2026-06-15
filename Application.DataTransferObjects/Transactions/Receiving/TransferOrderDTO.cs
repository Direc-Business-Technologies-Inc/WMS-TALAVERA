using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class TransferOrderDTO
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string FromSubsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public List<TransferOrderLineDTO> Lines { get; set; } = [];
}