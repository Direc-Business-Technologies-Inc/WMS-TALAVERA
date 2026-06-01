using Application.DataTransferObjects.Others;
using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class TransactionTypeHandler(ISender Sender) : ITransactionTypeHandler
{
    public async Task<IEnumerable<TransactionTypeVM>> GetTransactionTypesAsync()
    {
        GetTransactionTypeQry qry = new();
        IEnumerable<TransactionTypeDTO> response = await Sender.Send(qry);

        return response.Adapt<IEnumerable<TransactionTypeVM>>();
    }
}
