using Application.DataTransferObjects.Transactions.Commons.NS;

namespace Application.DataTransferObjects.Transactions.Commons.NS.Request;

public class SaveTransferOrderRequestDTO
{
    public List<PostTransferOrderDTO> PostTransferOrder { get; set; }
    public int TONetsuiteOrderInternalId { get; set; }
    public int UserId { get; set; }
}
