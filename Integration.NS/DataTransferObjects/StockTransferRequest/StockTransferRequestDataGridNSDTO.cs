using Application.DataTransferObjects.Transactions.StockTransferRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.StockTransferRequest;

public class StockTransferRequestDataGridNSDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateLastModified { get; set; }

    public bool IsEditable => StatusId == 3;
}
