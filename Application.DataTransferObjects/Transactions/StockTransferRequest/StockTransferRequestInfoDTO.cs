using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public class StockTransferRequestInfoDTO
{
    public string Status { get; set; } = string.Empty;
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public VendorDTO? Vendor { get; set; } = null;
    public LocationDTO? SourceLocation { get; set; } = null;
    public LocationDTO? DestinationLocation { get; set; } = null;
    public SubsidiaryDTO? Subsidiary { get; set; } = null;
    public SubsidiaryDTO? ToSubsidiary { get; set; } = null;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Type { get; set;} = string.Empty;
    public DateTime Date { get; set;}
    public List<StockTransferRequestLineDTO> Lines { get; set; } = [];
}
