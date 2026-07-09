using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Commands.Transaction.InventoryAdjustment;
using Application.UseCases.Queries.Transaction.InventoryAdjustment;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.InventoryAdjustment;

public class InventoryAdjustmentHandler(
    AppAuthenticationService authService,
    ISender sender) : IInventoryAdjustmentHandler
{
    public async Task<InventoryAdjustmentVM?> GetInventoryAdjustmentAsync(string id)
    {
        GetInventoryAdjustmentQry query = new(id);

        var dto = await sender.Send(query);
        if (dto is null) return null;

        var vm = dto.Adapt<InventoryAdjustmentVM>();

        foreach (var line in vm.Lines)
        {
            line.Type = line.QuantityAlloted < 0 ?
                InventoryAdjustmentLineVM.Types.Issue :
                InventoryAdjustmentLineVM.Types.Receipt;

            line.QuantityAlloted = Math.Abs(line.QuantityAlloted);
        }
        return vm;
    }

    public async Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetInventoryAdjustmentsDataGridAsync(DataGridIntent intent)
    {
        GetInventoryAdjustmentsQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryAdjustmentDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetIssuesDataGridAsync(DataGridIntent intent)
    {
        GetIssuesQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryAdjustmentDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetReceiptsDataGridAsync(DataGridIntent intent)
    {
        GetReceiptsQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryAdjustmentDataGridVM>>(), count);
    }

    public async Task<bool> CreateInventoryAdjustmentAsync(InventoryAdjustmentVM vm)
    {
        var dto = vm.Adapt<InventoryAdjustmentDTO>();
        dto.Lines.Clear();
        foreach (var line in vm.Lines)
        {
            var dtoline = line.Adapt<InventoryAdjustmentLineDTO>();
            dtoline.QuantityAlloted = line.Type == InventoryAdjustmentLineVM.Types.Issue ?
                -line.QuantityAlloted :
                line.QuantityAlloted;
            dto.Lines.Add(dtoline);
        }

        if (int.TryParse(authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeId"), out int employeeId))
        {
            dto.PreparedById = employeeId;
        }

        CreateInventoryAdjustmentCmd cmd = new(dto);
        return await sender.Send(cmd);
    }

    public async Task<(IEnumerable<InventoryAdjustmentReasonVM> Data, int Count)> GetInventoryAdjustmentReasonsAsync(DataGridIntent intent)
    {
        GetInventoryAdjustmentReasonsQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryAdjustmentReasonVM>>(), count);
    }

    public async Task<(IEnumerable<InventoryAdjustmentCategoryVM> Data, int Count)> GetInventoryAdjustmentCategoriesAsync(DataGridIntent intent)
    {
        GetInventoryAdjustmentCategoriesQry query = new GetInventoryAdjustmentCategoriesQry(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<InventoryAdjustmentCategoryVM>>(), count);
    }
}
