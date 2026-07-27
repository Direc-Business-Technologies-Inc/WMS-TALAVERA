using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;
using Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Libraries.ViewModel.TransferOrder;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Route("api/Receiving/[controller]")]
public class TransferOrderController(ISender Sender) : ControllerBase
{
    [HttpPost("PendingReceipt")]
    public async Task<ApiResult<IEnumerable<TransferOrderVM>>> GetAllTO(RequestPerSubsidiaryDTO req)
    {
        var result = await Sender.Send(new GetTransferOrdersQry(req));

        List<TransferOrderVM> ret = result.Adapt<List<TransferOrderVM>>();

        return ApiResult<IEnumerable<TransferOrderVM>>.Succeeded(ret);
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
