using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Application.UseCases.Queries.Transaction.SupplierReturn;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System.Linq;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.SupplierReturn;

public class SupplierReturnHandler(
    AppAuthenticationService authService,
    ISender sender,
    IAuthorizationService authorizationService) : ISupplierReturnHandler
{
    public async Task<BulkFulfillmentResultDTO> SubmitBulkFulfillmentAsync(SubmitBulkFulfillmentDTO data, CancellationToken ct = default)
    {
        if (!authService.IsAuthenticated() || !(await authorizationService.AuthorizeAsync(
            authService.CurrentUser, null, "permission.OSRN.CREATE")).Succeeded)
            return BulkFulfillmentResultDTO.Reject("You do not have permission to submit bulk fulfillment requests.");

        int[] subsidiaries;
        try
        {
            subsidiaries = JsonSerializer.Deserialize<int[]>(authService.GetClaimValue("com.direcbusiness.wms.nsAllowedSubsidiaries")) ?? [];
        }
        catch (JsonException)
        {
            subsidiaries = [];
        }
        if (subsidiaries.Length == 0 || subsidiaries.Any(id => id <= 0))
            return BulkFulfillmentResultDTO.Reject("Your subsidiary access could not be established. Sign in again or contact an administrator.");
        if (!Guid.TryParse(authService.GetUserId(), out var userId) || userId == Guid.Empty)
            return BulkFulfillmentResultDTO.Reject("Your user identity could not be established. Sign in again before submitting.");
        return await sender.Send(new SubmitBulkFulfillmentCmd(new(data.ReturnIds?.ToArray() ?? []), subsidiaries, userId), ct);
    }

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
