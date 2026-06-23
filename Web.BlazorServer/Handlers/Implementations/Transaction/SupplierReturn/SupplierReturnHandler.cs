using Application.UseCases.Queries.Transaction.SupplierReturn;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.SupplierReturn;

public class SupplierReturnHandler(ISender sender) : ISupplierReturnHandler
{
    public Task<SupplierReturnVM?> GetReturnAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<ReturnCategoryVM> Data, int Count)> GetReturnCategories(DataGridIntent intent)
    {
        GetReturnCategoriesQry query = new GetReturnCategoriesQry(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<ReturnCategoryVM>>(), count);
    }

    public Task<IEnumerable<SupplierReturnLineVM>> GetReturnLinesAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<SupplierReturnDataGridVM> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent)
    {
        GetSupplierReturnsDataGridQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<SupplierReturnDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<ReturnStatusVM> Data, int Count)> GetReturnStatuses(DataGridIntent intent)
    {
        GetReturnStatusesQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<ReturnStatusVM>>(), count);
    }
}
