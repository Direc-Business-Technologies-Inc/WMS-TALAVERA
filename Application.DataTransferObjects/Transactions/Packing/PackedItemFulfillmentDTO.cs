using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Packing
{
    public class PackedItemFulfillmentDTO
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime DateLastModified { get; set; }
        public string CreatedFrom { get; set; } = string.Empty;
        public string TransferCategory { get; set;} = string.Empty;
    }
}
