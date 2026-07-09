using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class TransferOrderDataGridDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SourceSubsidiary { get; set; } = string.Empty;
    public string DestinationSubsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
