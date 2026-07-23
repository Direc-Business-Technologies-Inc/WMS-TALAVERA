using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System.Text.Json;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class SubsidiaryHandler(IHttpContextAccessor contextAccessor, ISender sender) : ISubsidiaryHandler
{
    public async Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent)
    {
        GetSubsidiariesQry qry = new(intent);
        (var data, int count) = await sender.Send(qry);
        return (data.Adapt<IEnumerable<SubsidiaryVM>>(), count);
    }
    public async Task<SubsidiaryVM?> GetSubsidiaryAsync(int id)
    {
        GetSubsidiaryQry qry = new(id);
        var data = await sender.Send(qry);
        if (data is null) return null;

        return data.Adapt<SubsidiaryVM>();
    }
    public async Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetCurrentUserSubsidiariesAsync(DataGridIntent intent)
    {
        var newIntent = intent.Adapt<DataGridIntent>();

        string? claimValue = contextAccessor.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedSubsidiaries")?.Value;
        int[] userSubsidiaries = claimValue is null ? [] : JsonSerializer.Deserialize<List<int>>(claimValue)?.ToArray() ?? [];

        newIntent.Filters.Add(
            DataGridFilterUtilities.In(nameof(SubsidiaryVM.Id), userSubsidiaries)
        );

        GetSubsidiariesQry qry = new(newIntent);
        (var data, int count) = await sender.Send(qry);
        return (data.Adapt<IEnumerable<SubsidiaryVM>>(), count);
    }

    public async Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesByVendorAsync(DataGridIntent intent, int vendorId)
    {
        GetSubsidiariesByVendorQry qry = new(intent, vendorId);
        (var data, int count) = await sender.Send(qry);
        return (data.Adapt<IEnumerable<SubsidiaryVM>>(), count);
    }
    public async Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesByCustomerAsync(DataGridIntent intent, int customerId)
    {
        GetSubsidiariesByCustomerQry query = new(intent, customerId);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<SubsidiaryVM>>(), count);
    }
}
