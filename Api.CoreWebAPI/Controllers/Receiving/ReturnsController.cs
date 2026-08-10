using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.Returns;
using Application.UseCases.Queries.Transaction.Receiving.NS.Returns;
using Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Libraries.ViewModel.Returns;

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
