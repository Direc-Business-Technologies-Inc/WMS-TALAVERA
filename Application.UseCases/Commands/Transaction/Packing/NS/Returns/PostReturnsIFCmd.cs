using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Packing.NS.Returns;

public record PostReturnsIFCmd(List<PostReturnsDTO> Data) : ITransactionalRequest<bool>;

public class PostReturnsIFCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostReturnsIFCmd, bool>
{
    public async Task<bool> Handle(PostReturnsIFCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveReturnsItemFulfillment(request.Data);

        return result;
    }
}