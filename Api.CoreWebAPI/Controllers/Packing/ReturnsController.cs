using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Commands.Transaction.Packing.NS.Returns;
using Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;
using Application.UseCases.Queries.Transaction.Packing.NS.Returns;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.Returns;

namespace Api.CoreWebAPI.Controllers.Packing;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("api/Packing/[controller]")]
public class ReturnsController(ISender Sender) : ControllerBase
{
    [HttpPost("PendingFulfillment")]
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
        if (!int.TryParse(User.FindFirst("NsSubsidiaryId")?.Value, out var subsidiaryId) || subsidiaryId <= 0) return Unauthorized(ApiResult<ReturnsVM>.Failed("Authenticated NetSuite subsidiary identity is unavailable."));
        var user = new RequestPerUserDTO { NetsuiteUserSubsidiaryInternalId = subsidiaryId };
        var result = await Sender.Send(new GetPackingReturnByNumberQry(req.OrderNumber, user));
        if (result is null) return Ok(ApiResult<ReturnsVM>.NotFound("No eligible return was found."));
        return Ok(ApiResult<ReturnsVM>.Succeeded(result.Adapt<ReturnsVM>()));
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
    public async Task<IActionResult> ReturnsSaveScan(List<PostReturnsDTO> req)
    {
        ApiResult<bool> result = await Sender.Send(new PostReturnsIFCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
