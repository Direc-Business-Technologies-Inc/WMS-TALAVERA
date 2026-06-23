using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class ReceivingDTO 
{
    public ReceivingInfoDTO DocumentInfo { get; set; } = new();
    public List<ReceivingLineDTO> DocumentLines { get; set; } = [];
}
