using Application.DataTransferObjects.Commons;
using Application.DataTransferObjects.System.Commons;
using Domain.Enums.Transaction.Commons;

namespace Application.DataTransferObjects.Transactions.Commons;

public class TransactionalDocumentDTO : AuditableDTO
{
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.None;
    public AppDocNumDTO AppDocNum { get; set; } = new();
    public SapDocumentReferenceDTO SapReference { get; set; } = new();
    public DocumentTypeDTO DocumentType { get; set; } = new();
}
