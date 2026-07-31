using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Commands.Transaction.StockTransferRequest;
using Application.UseCases.Queries.Transaction.StockTransferRequest;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.StockTransferRequest;

public class StockTransferRequestHandler(
    AppAuthenticationService authService,
    ISender sender) : IStockTransferRequestHandler
{
    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetInterCompanyTransferOrdersList(DataGridIntent intent)
    {

        GetIntercompanyTransferOrderListQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetReturnsList(DataGridIntent intent)
    {
        GetReturnsListQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), count);
    }

    public async Task<StockTransferRequestInfoVM?> GetStockTransferRequest(string reference, bool includeLines = true)
    {
        GetStockTransferRequestQry query = new(reference);
        var dto = await sender.Send(query);
        var vm = dto.Adapt<StockTransferRequestInfoVM>();

        vm.Category = dto.TransferCategory;
        return vm;
    }

    public Task<(IEnumerable<StockTransferRequestLineVM> data, int count)> GetStockTransferRequestLines(string reference, DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetStockTransferRequestsList(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetTransferOrdersList(DataGridIntent intent)
    {
        GetTransferOrderListQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<TransferOrderStatusVM> data, int count)> GetTransferOrderStatuses(DataGridIntent intent)
    {
        GetTransferOrderStatusesQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<TransferOrderStatusVM>>(), count);
    }

    public async Task<bool> CreateStockTransferRequest(StockTransferRequestInfoVM data)
    {
        var dto = data.Adapt<StockTransferRequestInfoDTO>();
        dto.TransferCategory = data.Category;
        if (int.TryParse(authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeId"), out int employeeId))
        {
            dto.PreparedById = employeeId;
        }
        CreateStockTransferRequestCmd cmd = new(dto);

        await sender.Send(cmd);
        return true;
    }

    public async Task<bool> UpdateStockTransferRequest(StockTransferRequestInfoVM data)
    {
        var dto = data.Adapt<StockTransferRequestInfoDTO>();
        dto.TransferCategory = data.Category;
        UpdateStockTransferRequestCmd cmd = new(dto);

        await sender.Send(cmd);
        return true;
    }

    public async Task<bool> SubmitStockTransferRequestForApproval(StockTransferRequestInfoVM data)
    {
        var dto = data.Adapt<StockTransferRequestInfoDTO>();
        dto.TransferCategory = data.Category;
        SubmitStockTransferRequestForApprovalCmd cmd = new(dto);

        await sender.Send(cmd);
        return true;
    }
}
