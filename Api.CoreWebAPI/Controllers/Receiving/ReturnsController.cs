using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.Returns;
using Application.UseCases.Queries.Transaction.Receiving.NS.Returns;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Route("api/[controller]")]
public class ReturnsController(ISender Sender) : ControllerBase
{
    [HttpGet("PendingReceipt")]
    public async Task<ApiResult<IEnumerable<ReturnsVM>>> GetAllReturns()
    {
        var result = await Sender.Send(new GetReturnsQry());

        List<ReturnsVM> ret = result.Adapt<List<ReturnsVM>>();

        return ApiResult<IEnumerable<ReturnsVM>>.Succeeded(ret);
    }

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<ReturnsLineVM>>> ReturnsItems(ReturnsLineRequestDTO req)
    {
        var result = await Sender.Send(new GetReturnsLineQry(req));

        List<ReturnsLineVM> ret = result.Adapt<List<ReturnsLineVM>>();

        return ApiResult<IEnumerable<ReturnsLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<ApiResult> ReturnsSaveScan(List<PostReturnsDTO> req)
    {
        await Sender.Send(new PostReturnsCmd(req));

        return ApiResult.Succeeded();
    }
}
