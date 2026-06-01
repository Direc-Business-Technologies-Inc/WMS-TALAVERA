using Application.DataTransferObjects.Others;
using Domain.Entities.Enums.Transaction.Commons;

namespace Application.DataTransferObjects.Transactions.Commons;

public class MarketingDocumentDTO : TransactionalDocumentDTO
{
    public DateTime DocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    public DocumentStatus DocStatus { get; set; }
    public string Remarks { get; set; }
    public BusinessPartnerDTO BusinessPartner { get; set; }
}
