using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;
using Application.UseCases.Queries.Transaction.Receiving.NS.PurchaseOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.PurchaseOrder;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("api/Receiving/[controller]")]
public class PurchaseOrderController(ISender Sender) : ControllerBase
{
    [HttpPost("PendingReceipt")]
    public async Task<ApiResult<IEnumerable<PurchaseOrderVM>>> GetAllPO(RequestPerUserDTO req)
    {
        var result = await Sender.Send(new GetPurchaseOrdersQry(req));

        List<PurchaseOrderVM> ret = result.Adapt<List<PurchaseOrderVM>>();

        return ApiResult<IEnumerable<PurchaseOrderVM>>.Succeeded(ret);
    }

    [HttpPost("Lookup")]
    public async Task<IActionResult> Lookup(RequestDTO req)
    {
        if (string.IsNullOrWhiteSpace(req.OrderNumber))
            return StatusCode(400, ApiResult<PurchaseOrderVM>.Failed("Enter a purchase order number."));

        if (!TryGetNetSuiteIdentity(out var user)) return Unauthorized(ApiResult<PurchaseOrderVM>.Failed("Authenticated NetSuite employee identity is unavailable."));
        var result = await Sender.Send(new GetPurchaseOrderByNumberQry(req.OrderNumber, user));
        if (result is null)
            return Ok(ApiResult<PurchaseOrderVM>.NotFound("No eligible purchase order was found."));

        return Ok(ApiResult<PurchaseOrderVM>.Succeeded(result.Adapt<PurchaseOrderVM>()));
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

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<PurchaseOrderLineVM>>> PurchaseOrderItems(PurchaseOrderLineRequestDTO req)
    {
        var result = await Sender.Send(new GetPurchaseOrderLineQry(req));

        List<PurchaseOrderLineVM> ret = result.Adapt<List<PurchaseOrderLineVM>>();

        return ApiResult<IEnumerable<PurchaseOrderLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> PurchaseOrderSaveScan(SavePurchaseOrderRequestDTO req)
    {
        ApiResult<bool> result = await Sender.Send(new PostPurchaseOrderIRCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
