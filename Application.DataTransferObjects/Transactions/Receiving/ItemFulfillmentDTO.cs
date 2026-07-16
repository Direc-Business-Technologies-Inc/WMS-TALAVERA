using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class ItemFulfillmentDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
}

public class ItemFulfillmentLineDTO
{
    public int ItemFullfillmentId { get; set; }
    public int LineNumber { get; set; }
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal QuantityAlloted { get; set; }
    public decimal QuantityOpen { get; set; }
}
