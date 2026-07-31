using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Commands.Transaction.Packing.NS.Returns;
using Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;
using Application.UseCases.Queries.Transaction.Packing.NS.Returns;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.Returns;

namespace Api.CoreWebAPI.Controllers.Packing;

[ApiController]
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
