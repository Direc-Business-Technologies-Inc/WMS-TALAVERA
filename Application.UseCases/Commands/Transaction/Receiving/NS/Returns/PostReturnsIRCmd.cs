using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.Returns;

public record PostReturnsIRCmd(List<PostReturnsDTO> Data) : ITransactionalRequest<bool>;

public class PostReturnsIRCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostReturnsIRCmd, bool>
{
    public async Task<bool> Handle(PostReturnsIRCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveReturnsItemReceipt(request.Data);

        return result;
    }
}