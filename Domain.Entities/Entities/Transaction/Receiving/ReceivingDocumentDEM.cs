using Ardalis.GuardClauses;
using Domain.Entities.Entities.Transaction.Common;
using Domain.Entities.Enums.Transaction.Commons;
using Domain.Entities.ValueObjects.Others;
using Domain.ValueObjects.Transaction;

namespace Domain.Entities.Entities.Transaction.Receiving;

public class ReceivingDocumentDEM : MarketingDocumentDEM
{
    public string ReceivedBy { get; private set; }

    public ReceivingDocumentDEM() { }

    public ReceivingDocumentDEM(Guid documentType,
                                AppDocNumVO appDocNums,
                                DateTime docDate,
                                DateTime docDueDate,
                                DocumentStatus docStatus,
                                BusinessPartnerVO businessPartner,
                                string receivedBy,
                                string remarks = "",
                                SapDocumentReferenceVO? sapDocNums = null) : base(documentType,
                                                                                  appDocNums,
                                                                                  docDate,
                                                                                  docDueDate,
                                                                                  docStatus,
                                                                                  businessPartner,
                                                                                  remarks,
                                                                                  sapDocNums)
    {
        ReceivedBy = Guard.Against.NullOrEmpty(receivedBy, nameof(ReceivedBy), "Received By cannot be null or empty");
    }

    public ReceivingDocumentDEM UpdateReceivedBy(string receivedBy)
    {
        ReceivedBy = Guard.Against.NullOrEmpty(receivedBy, nameof(ReceivedBy), "Received By cannot be null or empty");
        return this;
    }
}
