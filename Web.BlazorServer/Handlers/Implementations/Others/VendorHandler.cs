using Application.UseCases.Queries.Others;
using Application.UseCases.Queries.Others.Vendor;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class VendorHandler(ISender sender) : IVendorHandler
{
    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetVendorsListAsync(DataGridIntent intent)
    {
        GetVendorsListQry query = new(intent);
        (var data, int count)  = await sender.Send(query);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }
    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetVendorsListBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId)
    {
        GetVendorsListBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count)  = await sender.Send(query);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetTradeVendorsListBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId)
    {
        GetTradeVendorsListBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetTradeVendorsListAsync(DataGridIntent intent)
    {
        GetTradeVendorsListQry query = new(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetNonTradeVendorsListBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId)
    {
        GetNonTradeVendorsListBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetNonTradeVendorsListAsync(DataGridIntent intent)
    {
        GetNonTradeVendorsListQry query = new(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }
}
