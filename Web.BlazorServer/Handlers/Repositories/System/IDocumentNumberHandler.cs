using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Handlers.Repositories.System;

public interface IDocumentNumberHandler
{
    Task<DocumentNumberVM> GetDocumentNumberAsync(Guid documentTypeId);
    Task<bool> UpdateDocumentNumberAsync(Guid documentTypeId);
    Task<DocumentNumberVM> GetDocumentNumberAsync(string documentTypeName);
    Task<bool> UpdateDocumentNumberAsync(string documentTypeName);
}
