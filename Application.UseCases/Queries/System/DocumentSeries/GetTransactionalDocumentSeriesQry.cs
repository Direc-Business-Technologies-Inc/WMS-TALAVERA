using Application.DataTransferObjects.System;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.System;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.System.DocumentSeries;

public record GetTransactionalDocumentSeriesQry(Guid DocumentTypeId) : IRequest<DocumentNumberDTO>;

public class GetTransactionalDocumentSeriesQryHandler(
    IAppReadRepository appReadRepo
    ) : IRequestHandler<GetTransactionalDocumentSeriesQry, DocumentNumberDTO>
{
    public async Task<DocumentNumberDTO> Handle(GetTransactionalDocumentSeriesQry request, CancellationToken cancellationToken)
    {
        DocumentNumberDEM? dem = await appReadRepo.FirstOrDefaultAsync<DocumentNumberDEM>(x => x.DocumentTypeId == request.DocumentTypeId);
        
        if (dem == null)
            throw new Exception("Document Series not found.");

        DocumentNumberDTO dto = dem.Adapt<DocumentNumberDTO>();
        dto.CurrentDocNum = dem.GenerateCurrentDocNum();
        dto.NextDocNum = dem.GenerateNextDocNum();
        return dto;
    }
}   
