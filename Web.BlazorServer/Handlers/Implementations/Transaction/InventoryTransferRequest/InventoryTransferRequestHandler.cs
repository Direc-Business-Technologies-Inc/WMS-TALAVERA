using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Commands.Transaction.InventoryTransferRequest;
using Application.UseCases.Queries.Transaction.InventoryTransferRequests;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransferRequest;

public class InventoryTransferRequestHandler(
    AppAuthenticationService authService,
    ISender sender) : IInventoryTransferRequestHandler
{
    public async Task<InventoryTransferRequestVM?> GetInventoryTransferRequestAsync(string Ref)
    {
        GetInventoryTransferRequestQry query = new(Ref);

        var response = await sender.Send(query);
        if (response is null) return null;

        return response.Adapt<InventoryTransferRequestVM>();
    }

    public async Task<(IEnumerable<InventoryTransferRequestDataGridVM> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent)
    {
        GetInventoryTransferRequestDataGridQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryTransferRequestDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<InventoryTransferRequestStatusVM> Data, int Count)> GetInventoryTransferRequestsStatusesAsync(DataGridIntent intent)
    {
        GetInventoryTransferRequestStatusesQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryTransferRequestStatusVM>>(), count);
    }
    public async Task<bool> CreateInventoryTransferRequest(InventoryTransferRequestVM data)
    {
        var dto = data.Adapt<InventoryTransferRequestDTO>();
        if (int.TryParse(authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeId"), out int employeeId))
        {
            dto.PreparedById = employeeId;
        }

        CreateInventoryTransferRequestCmd cmd = new(dto);

        return await sender.Send(cmd);
    }
    public async Task<bool> UpdateInventoryTransferRequest(InventoryTransferRequestVM data)
    {
        var dto = data.Adapt<InventoryTransferRequestDTO>();
        if (int.TryParse(authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeId"), out int employeeId))
        {
            dto.PreparedById = employeeId;
        }

        UpdateInventoryTransferRequestCmd cmd = new(dto);

        return await sender.Send(cmd);
    }

}
