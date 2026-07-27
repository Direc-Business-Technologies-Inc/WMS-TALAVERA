using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Transaction;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.NS
{
    public record GetNetsuiteIdentityQry(int id) : IRequest<NetsuiteIdentityDTO?>;

    public class GetNetsuiteIdentityQryHandler(
        INetsuiteIdentityIntegration integration) : IRequestHandler<GetNetsuiteIdentityQry, NetsuiteIdentityDTO?>
    {
        public Task<NetsuiteIdentityDTO?> Handle(GetNetsuiteIdentityQry request, CancellationToken cancellationToken)
        {
            return integration.GetNetsuiteIdentityAsync(request.id);
        }
    }
}
