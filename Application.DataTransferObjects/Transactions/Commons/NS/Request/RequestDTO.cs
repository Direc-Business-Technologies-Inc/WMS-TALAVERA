using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Commons.NS.Request;

public class RequestDTO
{
    public string OrderNumber { get; set; }
}

public class RequestPerSubsidiaryDTO
{
    public int NetsuiteUserSubsidiaryInternalId { get; set; }
}