using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class BusinessAccountHandler(ISender sender) : IBusinessAccountHandler
{
    public async Task<(IEnumerable<BusinessAccountVM> Data, int Count)> GetBusinessAccountsBySubsidiaryDataGridAsync(DataGridIntent intent, int subsidiary)
    {
        GetBusinessAccountsDataGridQry query = new(intent, subsidiary);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<BusinessAccountVM>>(),  count);
    }

    public async Task<(IEnumerable<BusinessAccountVM> Data, int Count)> GetBusinessAccountsDataGridAsync(DataGridIntent intent)
    {
        GetBusinessAccountsDataGridQry query = new(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<BusinessAccountVM>>(), count);
    }
}
