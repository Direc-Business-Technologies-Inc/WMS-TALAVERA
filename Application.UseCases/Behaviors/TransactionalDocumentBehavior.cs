using Application.UseCases.Repositories.Bases;
using MediatR;

namespace Application.UseCases.Behaviors;

public class TransactionalDocumentBehavior<TRequest, TResponse>(
    IAppCommandRepository appCommandRepo)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {

        if (appCommandRepo.IsInTransaction())
            return await next(cancellationToken);

        try
        {
            await appCommandRepo.BeginTransactionAsync();

            var response = await next(cancellationToken);

            await appCommandRepo.SaveChangesAsync();
            await appCommandRepo.CommitAsync();

            return response;
        }
        catch (Exception)
        {
            await appCommandRepo.RollbackAsync();
            throw;
        }

    }
}
