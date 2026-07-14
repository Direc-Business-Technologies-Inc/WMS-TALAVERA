using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseOrderDataGridDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DeliveryDate { get; set; }
}
