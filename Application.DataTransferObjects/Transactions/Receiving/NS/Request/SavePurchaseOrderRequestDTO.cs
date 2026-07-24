using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Request;

public class SavePurchaseOrderRequestDTO
{
    public List<PostPurchaseOrderDTO> PostPurchaseOrders { get; set; }
    public int UserId { get; set; }
}
