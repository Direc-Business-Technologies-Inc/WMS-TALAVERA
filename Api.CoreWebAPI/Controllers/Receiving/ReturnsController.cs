using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;
using Application.UseCases.Commands.Transaction.Receiving.NS.Returns;
using Application.UseCases.Queries.Transaction.Receiving.NS.Returns;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.Returns;
using Sprache;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
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
