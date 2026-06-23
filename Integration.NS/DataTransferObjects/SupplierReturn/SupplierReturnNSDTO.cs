using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.SupplierReturn;

public class SupplierReturnNSDTO
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorId { get; set; } 
    public string LocationName { get; set; } = string.Empty;
    public int LocationId { get; set; } 
    public string StatusName { get; set; } = string.Empty;
    public string StatusId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; } 
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
}
