using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseOrderDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DeliveryDate { get; set; }
    public List<PurchaseOrderLineDTO> Lines { get; set; } = [];
}

public class PurchaseOrderLineDTO
{
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal QuantityPlanned { get; set; }
}
