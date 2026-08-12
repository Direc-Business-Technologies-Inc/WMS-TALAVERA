using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Application.UseCases.Queries.Transaction.SupplierReturn;
using Mapster;
using MediatR;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System.Linq;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.SupplierReturn;

public class SupplierReturnHandler(
    AppAuthenticationService authService,
    ISender sender) : ISupplierReturnHandler
{
    public async Task<bool> CreateSupplierReturnAsync(SupplierReturnVM data)
    {
        var dto = data.Adapt<SupplierReturnDTO>();
        if (int.TryParse(authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeId"), out int employeeId))
        {
            dto.PreparedById = employeeId;
        }
        CreateSupplierReturnCmd cmd = new(dto);
        return await sender.Send(cmd); 
    }

    public async Task<bool> UpdateSupplierReturnAsync(SupplierReturnVM data)
    {
        var dto = data.Adapt<SupplierReturnDTO>();
        if (int.TryParse(authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeId"), out int employeeId))
        {
            dto.PreparedById = employeeId;
        }
        UpdateSupplierReturnCmd cmd = new(dto);
        return await sender.Send(cmd);
    }

    public async Task<bool> SubmitSupplierReturnForApproval(SupplierReturnVM data)
    {
        var dto = data.Adapt<SupplierReturnDTO>();
        if (int.TryParse(authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeId"), out int employeeId))
        {
            dto.PreparedById = employeeId;
        }
        SubmitSupplierReturnRequestCmd cmd = new(dto);
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

    public async Task<(IEnumerable<PurchaseCategoryVM> Data, int Count)> GetPurchaseCategoriesAsync(DataGridIntent intent)
    {
        GetPurchaseCategoriesQry query = new(intent);
        (var data, int count) = await sender.Send(query);
        return (data.Adapt<IEnumerable<PurchaseCategoryVM>>(), count);
    }

    public async Task<(IEnumerable<PurchaseSubcategoryVM> Data, int Count)> GetPurchaseSubCategoriesAsync(PurchaseCategoryVM category, DataGridIntent intent)
    {
        var newIntent = intent.Adapt<DataGridIntent>();
        newIntent.Filters.Add(
            DataGridFilterUtilities.Equal(nameof(PurchaseSubcategoryVM.PurchaseCategoryId), category.Id)
        );
        newIntent.Take = 10;

        GetPurchaseSubcategoriesQry query = new(newIntent);
        (var data, int count) = await sender.Send(query);

        if(category.Id == 3)
        {
            data = data.Where(x => x.Id == 5).ToList();
            count = data.Count();
        }

        return (data.Adapt<IEnumerable<PurchaseSubcategoryVM>>(), count);
    }
}
