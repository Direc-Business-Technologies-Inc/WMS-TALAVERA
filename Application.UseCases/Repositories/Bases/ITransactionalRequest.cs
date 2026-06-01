using MediatR;

namespace Application.UseCases.Repositories.Bases;

public interface ITransactionalRequest<TResponse> : IRequest<TResponse>
{
    
}
