using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class SubsidiaryHandler(ISender sender) : ISubsidiaryHandler
{
    public async Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent)
    {
        GetSubsidiariesQry qry = new(intent);
        (var data, int count) = await sender.Send(qry);
        return (data.Adapt<IEnumerable<SubsidiaryVM>>(), count);
    }
}
