using Ardalis.GuardClauses;
using Domain.Entities.Enums.Transaction.Commons;
using Domain.Entities.Transaction.Common;
using Domain.Entities.ValueObjects.Others;
using Domain.ValueObjects.Transaction;
using Microsoft.VisualBasic;

namespace Domain.Entities.Entities.Transaction.Common;

public abstract class MarketingDocumentDEM : TransactionalDocumentDEM
{
    public DateTime DocDate { get; private set; }
    public DateTime DocDueDate { get; private set; }
    public DocumentStatus DocStatus { get; private set; }
    public string Remarks { get; private set; }
    public BusinessPartnerVO BusinessPartner { get; private set; }

    public MarketingDocumentDEM() { }

    public MarketingDocumentDEM(Guid documentType,
                                AppDocNumVO appDocNums,
                                DateTime docDate,
                                DateTime docDueDate,
                                DocumentStatus docStatus,
                                BusinessPartnerVO businessPartner,
                                string remarks = "",
                                SapDocumentReferenceVO? sapDocNums = null) : base
                                    (documentType, appDocNums, sapDocNums)
    {
        DocDate = Guard.Against.OutOfSQLDateRange(docDate, nameof(DocDate), "Doc Date must be a valid Date");
        DocDueDate = Guard.Against.OutOfSQLDateRange(docDueDate, nameof(DocDueDate), "Doc Due Date must be a valid Date");
        DocStatus = Guard.Against.EnumOutOfRange<DocumentStatus>(docStatus, nameof(DocumentStatus), "Doc Status must be a valid Document Status");
        BusinessPartner = Guard.Against.Null(businessPartner, nameof(BusinessPartner), "Business Partner cannot be null");
        Remarks = remarks;
    }

    public MarketingDocumentDEM UpdateDocDate(DateTime docDate)
    {
        DocDate = Guard.Against.OutOfSQLDateRange(docDate, nameof(DocDate), "Doc Date must be a valid Date");
        return this;
    }

    public MarketingDocumentDEM UpdateDocDueDate(DateTime docDueDate)
    {
        DocDueDate = Guard.Against.OutOfSQLDateRange(docDueDate, nameof(DocDueDate), "Doc Due Date must be a valid Date");
        return this;
    }

    public MarketingDocumentDEM UpdateBusinessPartner(BusinessPartnerVO businessPartner)
    {
        BusinessPartner = Guard.Against.Null(businessPartner, nameof(BusinessPartner), "Business Partner cannot be null");
        return this;
    }

    public MarketingDocumentDEM UpdateRemarks(string remarks)
    {
        Remarks = remarks;
        return this;
    }

    public MarketingDocumentDEM UpdateDocumentStatus(DocumentStatus status)
    {
        DocStatus = status;
        return this;
    }
}
