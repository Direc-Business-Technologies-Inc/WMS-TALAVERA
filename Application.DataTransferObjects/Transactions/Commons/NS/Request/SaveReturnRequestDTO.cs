using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Commons.NS.Request;

public class SaveReturnRequestDTO
{
    public List<PostReturnsDTO> PostReturn { get; set; }
    public int TONetsuiteOrderInternalId { get; set; }
    public int UserId { get; set; }
}
