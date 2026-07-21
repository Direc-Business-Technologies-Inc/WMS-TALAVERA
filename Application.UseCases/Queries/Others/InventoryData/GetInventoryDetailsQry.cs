using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.InventoryData
{
    public record GetInventoryDetailsQry(int documentId, int lineId) : IRequest<IEnumerable<InventoryDetailDTO>>;

    public class GetInventoryDetailsQryHandler(IInventoryIntegration integration) : IRequestHandler<GetInventoryDetailsQry, IEnumerable<InventoryDetailDTO>>
    {
        public Task<IEnumerable<InventoryDetailDTO>> Handle(GetInventoryDetailsQry request, CancellationToken cancellationToken)
        {
            return integration.GetInventoryDetails(request.documentId, request.lineId);
        }
    }
}
