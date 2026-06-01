using Application.DataTransferObjects.Commons;

namespace Application.DataTransferObjects.Transactions.Commons;

public class DocumentTypeVM : EntityDTO
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
}
