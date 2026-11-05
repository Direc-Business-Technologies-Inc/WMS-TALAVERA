using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;
using Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Libraries.ViewModel.TransferOrder;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("api/Receiving/[controller]")]
public class TransferOrderController(ISender Sender) : ControllerBase
{
    [HttpPost("PendingReceipt")]
    public async Task<ApiResult<IEnumerable<TransferOrderVM>>> GetAllTO(RequestPerUserDTO req, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var result = await Sender.Send(new GetTransferOrdersQry(req, limit, offset));

        List<TransferOrderVM> ret = result.Adapt<List<TransferOrderVM>>();

        return ApiResult<IEnumerable<TransferOrderVM>>.Succeeded(ret);
    }

    [HttpPost("Lookup")]
    public async Task<IActionResult> Lookup(RequestDTO req)
    {
        if (string.IsNullOrWhiteSpace(req.OrderNumber)) return StatusCode(400, ApiResult<TransferOrderVM>.Failed("Enter a transfer order number."));
        if (!TryGetNetSuiteIdentity(out var user)) return Unauthorized(ApiResult<TransferOrderVM>.Failed("Authenticated NetSuite employee identity is unavailable."));
        var result = await Sender.Send(new GetReceivingTransferOrderByNumberQry(req.OrderNumber, user));
        if (result is null) return Ok(ApiResult<TransferOrderVM>.NotFound("No eligible transfer order was found."));
        return Ok(ApiResult<TransferOrderVM>.Succeeded(result.Adapt<TransferOrderVM>()));
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
    public async Task<ApiResult<IEnumerable<TransferOrderLineVM>>> TransferOrderItems(TransferOrderLineRequestDTO req)
    {
        var result = await Sender.Send(new GetTransferOrderLineQry(req));

        List<TransferOrderLineVM> ret = result.Adapt<List<TransferOrderLineVM>>();

        return ApiResult<IEnumerable<TransferOrderLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> TransferOrderSaveScan(SaveTransferOrderRequestDTO req)
    {
        ApiResult<bool> result = await Sender.Send(new PostTransferOrderIRCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
