using Application.DataTransferObjects.System.Commons;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Transaction.Common;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.System;

public record GetDocumentTypeByNameQry(string Name) : IRequest<DocumentTypeDTO>;

public class GetDocumentTypeByNameQryHandler(
    IAppReadRepository appReadRepo)
    : IRequestHandler<GetDocumentTypeByNameQry, DocumentTypeDTO>
{
    public async Task<DocumentTypeDTO> Handle(GetDocumentTypeByNameQry request, CancellationToken cancellationToken)
    {
        DocumentTypeDEM? dem = await appReadRepo.FirstOrDefaultAsync<DocumentTypeDEM>(x => x.Name.ToLower() == request.Name.ToLower()) ?? throw new Exception($"Document Type with named {request.Name} not found.");

        DocumentTypeDTO dto = dem.Adapt<DocumentTypeDTO>();

        return dto;
    }
}
