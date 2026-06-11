using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;
using Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Route("api/[controller]")]
public class TransferOrderController(ISender Sender) : ControllerBase
{
    [HttpGet("PendingReceipt")]
    public async Task<ApiResult<IEnumerable<TransferOrderVM>>> GetAllTO()
    {
        var result = await Sender.Send(new GetTransferOrdersQry());

        List<TransferOrderVM> ret = result.Adapt<List<TransferOrderVM>>();

        return ApiResult<IEnumerable<TransferOrderVM>>.Succeeded(ret);
    }

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<TransferOrderLineVM>>> TransferOrderItems(TransferOrderLineRequestDTO req)
    {
        var result = await Sender.Send(new GetTransferOrderLineQry(req));

        List<TransferOrderLineVM> ret = result.Adapt<List<TransferOrderLineVM>>();

        return ApiResult<IEnumerable<TransferOrderLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<ApiResult> TransferOrderSaveScan(List<PostTransferOrderDTO> req)
    {
        await Sender.Send(new PostTransferOrderCmd(req));

        return ApiResult.Succeeded();
    }
}
