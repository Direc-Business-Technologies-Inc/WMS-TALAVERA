using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.Returns;
using Application.UseCases.Queries.Transaction.Receiving.NS.Returns;
using Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Libraries.ViewModel.Returns;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("api/Receiving/[controller]")]
public class ReturnsController(ISender Sender) : ControllerBase
{
    [HttpPost("PendingReceipt")]
    public async Task<ApiResult<IEnumerable<ReturnsVM>>> GetAllReturns(RequestPerUserDTO req)
    {
        var result = await Sender.Send(new GetReturnsQry(req));

        List<ReturnsVM> ret = result.Adapt<List<ReturnsVM>>();

        return ApiResult<IEnumerable<ReturnsVM>>.Succeeded(ret);
    }

    [HttpPost("Lookup")]
    public async Task<IActionResult> Lookup(RequestDTO req)
    {
        if (string.IsNullOrWhiteSpace(req.OrderNumber)) return StatusCode(400, ApiResult<ReturnsVM>.Failed("Enter a return number."));
        if (!TryGetNetSuiteIdentity(out var user)) return Unauthorized(ApiResult<ReturnsVM>.Failed("Authenticated NetSuite employee identity is unavailable."));
        var result = await Sender.Send(new GetReceivingReturnByNumberQry(req.OrderNumber, user));
        if (result is null) return Ok(ApiResult<ReturnsVM>.NotFound("No eligible return was found."));
        return Ok(ApiResult<ReturnsVM>.Succeeded(result.Adapt<ReturnsVM>()));
    }

    private bool TryGetNetSuiteIdentity(out RequestPerUserDTO user)
    {
        var employee = User.FindFirst("NsEmployeeId")?.Value;
        var subsidiary = User.FindFirst("NsSubsidiaryId")?.Value;
        user = new RequestPerUserDTO();
        if (!int.TryParse(employee, out var employeeId) || employeeId <= 0 || !int.TryParse(subsidiary, out var subsidiaryId) || subsidiaryId <= 0) return false;
        user.NetsuiteUserInternalId = employeeId;
        user.NetsuiteUserSubsidiaryInternalId = subsidiaryId;
        return true;
    }

    [HttpPost("ItemFulfillment")]
    public async Task<ApiResult<IEnumerable<ItemFulfillmentVM>>> TOItemFulfillments(TransferOrderIFRequestDTO req)
    {
        var result = await Sender.Send(new GetTransferOrderIFQry(req));

        List<ItemFulfillmentVM> ret = result.Adapt<List<ItemFulfillmentVM>>();

        return ApiResult<IEnumerable<ItemFulfillmentVM>>.Succeeded(ret);
    }

    [HttpPost("ItemFulfillment/Items")]
    public async Task<ApiResult<IEnumerable<TOxItemFulfillmentLineVM>>> TOItemFulfillmentItems(TransferOrderIFLineRequestDTO req)
    {
        var result = await Sender.Send(new GetTransferOrderIFItemsQry(req));

        List<TOxItemFulfillmentLineVM> ret = result.Adapt<List<TOxItemFulfillmentLineVM>>();

        return ApiResult<IEnumerable<TOxItemFulfillmentLineVM>>.Succeeded(ret);
    }

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<ReturnsLineVM>>> ReturnsItems(ReturnsLineRequestDTO req)
    {
        var result = await Sender.Send(new GetReturnsLineQry(req));

        if (result is null || !result.Any())
        {
            return ApiResult<IEnumerable<ReturnsLineVM>>.Succeeded(Enumerable.Empty<ReturnsLineVM>());
        }

        List<ReturnsLineVM> ret = result.Adapt<List<ReturnsLineVM>>();

        return ApiResult<IEnumerable<ReturnsLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> ReturnsSaveScan(SaveReturnRequestDTO req)
    {
        ApiResult<bool> result = await Sender.Send(new PostReturnsIRCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
