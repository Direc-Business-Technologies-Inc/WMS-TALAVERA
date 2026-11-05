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
    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetVendorsListQry query = new(intent);
        (var data, int count)  = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }
    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetVendorsListBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId, CancellationToken ct = default)
    {
        GetVendorsListBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count)  = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetTradeVendorsListBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId, CancellationToken ct = default)
    {
        GetTradeVendorsListBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetTradeVendorsListQry query = new(intent);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetNonTradeVendorsListBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId, CancellationToken ct = default)
    {
        GetNonTradeVendorsListBySubsidiaryQry query = new(intent, subsidiaryId);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }

    public async Task<(IEnumerable<VendorVM> Data, int Count)> GetNonTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetNonTradeVendorsListQry query = new(intent);
        (var data, int count) = await sender.Send(query, ct);
        return (data.Adapt<IEnumerable<VendorVM>>(), count);
    }
}
