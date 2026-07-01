using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Application.UseCases.Queries.Transaction.SupplierReturn;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.SupplierReturn;

public class SupplierReturnHandler(ISender sender) : ISupplierReturnHandler
{
    public async Task<bool> CreateSupplierReturnAsync(SupplierReturnVM data)
    {
        CreateSupplierReturnCmd cmd = new(data.Adapt<SupplierReturnDTO>());
        return await sender.Send(cmd); 
    }

    public async Task<(IEnumerable<PurchaseOrderDataGridVM> Data, int Count)> GetPurchaseOrdersDataGridAsync(DataGridIntent intent)
    { 
        GetPurchaseOrderDataGridQry query = new(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<PurchaseOrderDataGridVM>>(), count);
    }

    public async Task<SupplierReturnVM?> GetReturnAsync(string Ref)
    {
        GetReturnQry query = new(Ref);

        var dto = await sender.Send(query);

        return dto?.Adapt<SupplierReturnVM>() ?? null;
    }

    public async Task<(IEnumerable<ReturnCategoryVM> Data, int Count)> GetReturnCategories(DataGridIntent intent)
    {
        GetReturnCategoriesQry query = new GetReturnCategoriesQry(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<ReturnCategoryVM>>(), count);
    }

    public async Task<SupplierReturnVM?> GetReturnFromPurchaseOrderAsync(string Ref)
    {
        GetReturnFromPurchaseOrderQry query = new(Ref);

        var dto = await sender.Send(query);

        return dto?.Adapt<SupplierReturnVM>() ?? null;
    }

    public Task<IEnumerable<SupplierReturnLineVM>> GetReturnFromPurchaseOrderLinesAsync(string Ref)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SupplierReturnLineVM>> GetReturnLinesAsync(string Ref)
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
