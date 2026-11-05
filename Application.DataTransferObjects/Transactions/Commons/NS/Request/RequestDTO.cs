using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Commons.NS.Request;

public class RequestDTO
{
    public string OrderNumber { get; set; } = string.Empty;
}

public class RequestPerUserDTO
{
    public int NetsuiteUserSubsidiaryInternalId { get; set; }
    public int NetsuiteUserInternalId { get; set; }
}

public class RequestTOxIFDTO
{
    public int NetsuiteOrderInternalId { get; set; }
}
