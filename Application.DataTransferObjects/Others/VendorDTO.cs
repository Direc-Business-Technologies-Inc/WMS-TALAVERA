using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others;

public class VendorDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public VendorCategoryDTO? Category { get; set; } 
}

public class VendorCategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}