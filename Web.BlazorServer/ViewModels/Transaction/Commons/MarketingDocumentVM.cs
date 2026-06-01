using Domain.Entities.Enums.Transaction.Commons;
using Domain.Providers;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.Commons;

public abstract class MarketingDocumentVM : TransactionalDocumentVM
{
    public DateTime DocDate { get; set; } = DateTimeProvider.Now;
    public DateTime DocDueDate { get; set; } = DateTimeProvider.Now;
    public DocumentStatus DocStatus { get; set; } = DocumentStatus.Open;
    public string Remarks { get; set; } = string.Empty;
    public BusinessPartnerVM BusinessPartner { get; set; } = new();
}
