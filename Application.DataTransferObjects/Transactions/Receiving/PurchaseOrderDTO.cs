using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseOrderDTO 
{
    public PurchaseOrderInfoDTO DocumentInfo { get; set; } = new();
    public List<PurchaseOrderLineDTO> DocumentLines { get; set; } = [];
}
