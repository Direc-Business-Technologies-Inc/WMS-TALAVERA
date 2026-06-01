using Ardalis.GuardClauses;
using Domain.Commons;
using Domain.Enums.Transaction.Commons;
using Domain.Markers;
using Domain.ValueObjects.Transaction;

namespace Domain.Entities.Transaction.Common;

public abstract class TransactionalDocumentDEM : AuditableDEM, ITransactionalDocument
{
    public ApprovalStatus ApprovalStatus { get; private set; }
    public AppDocNumVO AppDocNum { get; private set; }
    public SapDocumentReferenceVO? SapReference { get; private set; } = null;

    public Guid DocumentTypeId { get; private set; }

    protected TransactionalDocumentDEM()
    {
        ApprovalStatus = ApprovalStatus.None;
    }

    protected TransactionalDocumentDEM
    (
        Guid documentType,
        AppDocNumVO appDocNums,
        SapDocumentReferenceVO? sapDocNums = null
    )
    {
        DocumentTypeId = Guard.Against.NullOrEmpty(documentType, nameof(documentType), "Document type cannot be null");
        AppDocNum = Guard.Against.Null(appDocNums, nameof(appDocNums), "LSMS Document Series cannot be null");
        ApprovalStatus = ApprovalStatus.None;
    }

    /**
     * DEV: Charles Maverick Herrera
     * Date: September 05, 2025
     * Title: Setting Approval Status
     * Note: 
     *      by default, ApprovalStatus is None.
     *      This method only allows setting a new status that is greater than the current status.
     *      If the document is wished to be updated to another status, it can be done by setting it again to None first.
     */

    public virtual void SetApprovalStatus(ApprovalStatus approvalStatus)
    {
        if (approvalStatus != ApprovalStatus.None && approvalStatus <= ApprovalStatus)
            throw new InvalidDataException("New approval status must be greater than current status");

        ApprovalStatus = approvalStatus;
    }

    public TransactionalDocumentDEM UpdateAppDocNum(AppDocNumVO lsmsDocNum)
    {
        AppDocNum = Guard.Against.Null(lsmsDocNum, nameof(lsmsDocNum), "LSMS Document Series cannot be null");
        return this;
    }

    public TransactionalDocumentDEM UpdateSapDocRef(SapDocumentReferenceVO sapReference)
    {
        SapReference = Guard.Against.Null(sapReference, nameof(sapReference), "SAP Document Series cannot be null");
        return this;
    }
}
