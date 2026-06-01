using Application.DataTransferObjects.Transactions.Commons;
using Web.BlazorServer.ViewModels.Commons;

namespace Web.BlazorServer.ViewModels.System;

public class DocumentNumberVM : EntityVM
{
    public DocumentTypeVM DocumentType { get; set; }
    public string Code { get; set; }
    public string Prefix { get; set; }
    public int CurrentNumber { get; set; }
    public int NextNumber { get; set; }
    public string CurrentDocNum { get; set; }
    public string NextDocNum { get; set; }
}
