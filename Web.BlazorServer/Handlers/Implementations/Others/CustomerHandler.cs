using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class CustomerHandler(ISender sender) : ICustomerHandler
{
    public async Task<(IEnumerable<CustomerVM>, int)> GetCustomersListAsync(DataGridIntent intent)
    {
        GetCustomersQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<CustomerVM>>(), count);
    }
}
