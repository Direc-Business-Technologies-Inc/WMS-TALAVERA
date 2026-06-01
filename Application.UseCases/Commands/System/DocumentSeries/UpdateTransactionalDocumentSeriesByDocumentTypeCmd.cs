using Application.UseCases.Repositories.Bases;
using Domain.Entities.System;
using MediatR;

namespace Application.UseCases.Commands.System.DocumentSeries;

public record UpdateTransactionalDocumentSeriesByDocumentTypeCmd(Guid DocumentTypeId) : ITransactionalRequest<bool>;

public class UpdateTransactionalDocumentSeriesByDocumentTypeCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo
    ) : IRequestHandler<UpdateTransactionalDocumentSeriesByDocumentTypeCmd, bool>
{
    public async Task<bool> Handle(UpdateTransactionalDocumentSeriesByDocumentTypeCmd request, CancellationToken cancellationToken)
    {
        var series = await appReadRepo.FirstOrDefaultAsync<DocumentNumberDEM>(x => x.DocumentTypeId == request.DocumentTypeId, true);
        if (series is null)
            throw new Exception("Document series not found.");
        series.IncrementNumber();
        appCommandRepo.Update(series);
        return true;
    }
}
