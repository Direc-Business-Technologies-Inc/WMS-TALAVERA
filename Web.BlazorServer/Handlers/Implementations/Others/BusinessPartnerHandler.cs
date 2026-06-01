using Application.DataTransferObjects.Others;
using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class BusinessPartnerHandler(
    ISender Sender) 
    : IBusinessPartnerHandler
{
    public async Task<(IEnumerable<BusinessPartnerVM> Data, int Count)> GetCustomersAsync(DataGridIntent intent)
    {
        GetCustomersQry qry = new(intent);
        (IEnumerable<BusinessPartnerDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<BusinessPartnerVM>>(), Count);
    }

    public async Task<(IEnumerable<BusinessPartnerVM> Data, int Count)> GetAllAsync(DataGridIntent intent)
    {
        GetAllBusinessPartnersQry qry = new(intent);
        (IEnumerable<BusinessPartnerDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<BusinessPartnerVM>>(), Count);
    }

    public async Task<(IEnumerable<BusinessPartnerVM> Data, int Count)> GetVendorsAsync(DataGridIntent intent)
    {
        GetVendorsQry qry = new(intent);
        (IEnumerable<BusinessPartnerDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<BusinessPartnerVM>>(), Count);
    }
}
