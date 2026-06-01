using Application.UseCases.Repositories.Bases;
using Domain.Entities.System;
using MediatR;

namespace Application.UseCases.Commands.System.DocumentSeries;

public record UpdateTransactionalDocumentSeriesCmd(Guid SeriesId) : ITransactionalRequest<bool>;

public class UpdateTransactionalDocumentSeriesQryHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo
    ) : IRequestHandler<UpdateTransactionalDocumentSeriesCmd, bool>
{
    public async Task<bool> Handle(UpdateTransactionalDocumentSeriesCmd request, CancellationToken cancellationToken)
    {
        var series = await appReadRepo.FirstOrDefaultAsync<DocumentNumberDEM>(x => x.Id == request.SeriesId, true);
        if (series is null)
            throw new Exception("Document series not found.");
        series.IncrementNumber();
        appCommandRepo.Update(series);

        return true;
    }
}