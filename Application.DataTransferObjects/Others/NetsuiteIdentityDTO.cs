using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others;

public class NetsuiteIdentityDTO
{
    public int EmployeeID { get; set; }
    public int SubsidiaryID { get; set; }
    public int[] AllowedSubsidiaries { get; set; } = [];
    public string SubsidiaryName { get; set; } = string.Empty;
    public string EmployeeFirstName { get; set; } = string.Empty;
    public string EmployeeLastName { get; set; } = string.Empty;
    public string EmployeeFullName => EmployeeFirstName + " " + EmployeeLastName;
}
