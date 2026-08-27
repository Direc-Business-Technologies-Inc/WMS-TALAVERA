using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class ItemReceiptDataGridDTO
{
    public int Id { get; set;  }
    public DateTime Date { get; set; } 
    public DateTime DateLastModified { get; set; } 
    public string ReferenceNumber { get; set; } = string.Empty;
    public string TransferCategory { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string CreatedFrom { get; set; } = string.Empty;
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
}
