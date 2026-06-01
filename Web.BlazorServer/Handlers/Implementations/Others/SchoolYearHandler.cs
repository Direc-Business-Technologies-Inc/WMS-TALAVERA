using Application.DataTransferObjects.Others;
using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class SchoolYearHandler(
    ISender Sender) 
    : ISchoolYearHandler
{
    public async Task<(IEnumerable<SchoolYearVM> Data, int Count)> GetSchoolYearsAsync(DataGridIntent intent)
    {
        GetSchoolYearsQry qry = new(intent);
        (IEnumerable<SchoolYearDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<SchoolYearVM>>(), Count);
    }
}
