using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public class SupplierReturnDTO
{
    public int Id { get; set; }
    public int? PreparedById { get; set; }
    public DateTime Date { get; set; }
    public VendorDTO? Vendor { get; set; } = null;
    public LocationDTO? Location { get; set; } = null;
    public ReturnStatusDTO? Status { get; set; } = null;
    public ReturnCategoryDTO? ReturnCategory { get; set; } = null;
    public SubsidiaryDTO? Subsidiary { get; set; } = null;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public IEnumerable<SupplierReturnLineDTO> Lines { get; set; } = [];

    public int? SourcePO { get; set; }
}
