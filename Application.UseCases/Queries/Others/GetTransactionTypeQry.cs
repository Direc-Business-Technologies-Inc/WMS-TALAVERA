using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Others;

public record GetTransactionTypeQry() : IRequest<IEnumerable<TransactionTypeDTO>>;

public class GetTransactionTypeQryHandler(
    ITransactionTypeIntegration transactionTypeIntegration
    ) 
    : IRequestHandler<GetTransactionTypeQry, IEnumerable<TransactionTypeDTO>>
{
    public async Task<IEnumerable<TransactionTypeDTO>> Handle(GetTransactionTypeQry request, CancellationToken cancellationToken)
    {
        IEnumerable<TransactionTypeSAPDTO> response = await transactionTypeIntegration.GetTransactionTypesAsync();

        return response.Adapt<IEnumerable<TransactionTypeDTO>>();
    }
}

