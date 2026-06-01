using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetSchoolYearsQry(DataGridIntent Intent) : IRequest<(IEnumerable<SchoolYearDTO> Data, int Count)>;

public class GetSchoolYearsQryHandler(ISchoolYearIntegration schoolYearIntegration)
    : IRequestHandler<GetSchoolYearsQry, (IEnumerable<SchoolYearDTO> Data, int Count)>
{
    public async Task<(IEnumerable<SchoolYearDTO> Data, int Count)> Handle(GetSchoolYearsQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<SchoolYearSAPDTO> Data, int Count) = await schoolYearIntegration.GetAllSchoolYear(request.Intent);

        return (Data.Adapt<IEnumerable<SchoolYearDTO>>(), Count);
    }
}
