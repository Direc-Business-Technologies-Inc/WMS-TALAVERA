using Application.DataTransferObjects.System;
using Application.DataTransferObjects.System.Commons;
using Application.DataTransferObjects.Transactions.Commons;
using Application.UseCases.Commands.System.DocumentSeries;
using Application.UseCases.Queries.System;
using Application.UseCases.Queries.System.DocumentSeries;
using Mapster;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Handlers.Implementations.System;

public class DocumentNumberHandler(
    ISender Sender) 
    : IDocumentNumberHandler
{
    public async Task<DocumentNumberVM> GetDocumentNumberAsync(Guid documentTypeId)
    {
        GetTransactionalDocumentSeriesQry qry = new(documentTypeId);
        DocumentNumberDTO response = await Sender.Send(qry);

        return response.Adapt<DocumentNumberVM>();
    }

    public async Task<DocumentNumberVM> GetDocumentNumberAsync(string documentTypeName)
    {
        GetDocumentTypeByNameQry docTypeQry = new(documentTypeName);
        DocumentTypeDTO docTypeDTO = await Sender.Send(docTypeQry);

        GetTransactionalDocumentSeriesQry qry = new(docTypeDTO.Id);
        DocumentNumberDTO response = await Sender.Send(qry);

        return response.Adapt<DocumentNumberVM>();
    }

    public async Task<bool> UpdateDocumentNumberAsync(Guid documentTypeId)
    {
        UpdateTransactionalDocumentSeriesCmd cmd = new(documentTypeId);
        bool result = await Sender.Send(cmd);

        return result;
    }

    public async Task<bool> UpdateDocumentNumberAsync(string documentTypeName)
    {
        GetDocumentTypeByNameQry docTypeQry = new(documentTypeName);
        DocumentTypeDTO docTypeDTO = await Sender.Send(docTypeQry);

        UpdateTransactionalDocumentSeriesCmd cmd = new(docTypeDTO.Id);
        bool result = await Sender.Send(cmd);

        return result;
    }
}
