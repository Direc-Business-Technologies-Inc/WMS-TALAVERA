using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public class SupplierReturnDataGridDTO
{
    public string VendorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string CreatedFrom { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DateLastModified { get; set; }
}
