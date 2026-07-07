using Application.UseCases.Queries.Transaction.Packing.VendorReturnAuthorization;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Packing.VendorReturnAuthorization;

public class VendorReturnAuthorizationPackingHandler(ISender sender) : IVendorReturnAuthorizationPackingHandler
{
    public async Task<(IEnumerable<VendorReturnAuthorizationPackingDataGridVM> Data, int Count)> GetVendorReturnAuthorizationsList(DataGridIntent intent, int subsidiaryId)
    {
        GetPackingVendorReturnAuthorizationListQry query = new(intent, subsidiaryId);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<VendorReturnAuthorizationPackingDataGridVM>>(), count);
    }

    public async Task<VendorReturnAuthorizationInfoPackingVM?> GetPackingVendorReturnAuthorization(string reference)
    {
        GetPackingVendorReturnAuthorizationQry query = new(reference);

        var dto = await sender.Send(query);
        if (dto is null) return null;

        var vm = dto.Adapt<VendorReturnAuthorizationInfoPackingVM>();
        vm.SourceWarehouse = dto.Location;
        vm.DestinationWarehouse = dto.TransferLocation;

        return vm;
    }

    public async Task<(IEnumerable<VendorReturnAuthorizationLinePackingVM> Data, int Count)> GetPackingVendorReturnAuthorizationLines(string reference, DataGridIntent intent)
    {
        GetPackingVendorReturnAuthorizationLinesQry query = new(reference, intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<VendorReturnAuthorizationLinePackingVM>>(), count);
    }
}
