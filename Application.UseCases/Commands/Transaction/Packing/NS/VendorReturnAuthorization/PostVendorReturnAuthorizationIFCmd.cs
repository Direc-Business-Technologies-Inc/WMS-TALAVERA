using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Packing.NS.VendorReturnAuthorization;

public record PostVendorReturnAuthorizationIFCmd(List<PostVendorReturnAuthorizationDTO> Data) : ITransactionalRequest<bool>;

public class PostVendorReturnAuthorizationIFCmdHandler(INetSuiteApiClientService netSuiteApiClientService) : IRequestHandler<PostVendorReturnAuthorizationIFCmd, bool>
{
    public async Task<bool> Handle(PostVendorReturnAuthorizationIFCmd request, CancellationToken cancellationToken)
    {
        bool result = await netSuiteApiClientService.SaveVRAItemFulfillment(request.Data);

        return result;
    }
}