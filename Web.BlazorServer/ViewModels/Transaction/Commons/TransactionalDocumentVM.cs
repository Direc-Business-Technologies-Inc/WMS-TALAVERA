using Application.DataTransferObjects.System.Commons;
using Application.DataTransferObjects.Transactions.Commons;
using Domain.Enums.Transaction.Commons;
using Web.BlazorServer.ViewModels.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Commons;

public abstract class TransactionalDocumentVM : AuditableVM
{
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.None;
    public AppDocNumVM AppDocNum { get; set; } = new();
    public SapDocumentReferenceVM SapReference { get; set; } = new();
    public DocumentTypeVM DocumentType { get; set; } = new();
}
