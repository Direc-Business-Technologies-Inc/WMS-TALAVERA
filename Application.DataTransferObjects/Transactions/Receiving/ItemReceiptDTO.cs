using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class ItemReceiptDTO
{
    public string Type { get; set; } = string.Empty;
    public string CreatedFrom { get; set; } = string.Empty;
    public string ReceivingCategory { get; set; } = string.Empty;
    public string Department { get; set; } = "Operations";
    public string Vendor { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public List<ItemReceiptLineDTO> Lines = [];
}

public class ItemReceiptLineDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Department { get; set; } = "Operations";
    public string Location { get; set; } = string.Empty;
    public decimal WeightReceived { get; set; }
    public decimal WeightTotal { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityOpen { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBad { get; set; }
}