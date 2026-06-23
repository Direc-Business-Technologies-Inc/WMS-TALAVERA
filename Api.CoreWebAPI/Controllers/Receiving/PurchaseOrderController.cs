using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;
using Application.UseCases.Queries.Transaction.Receiving.NS.PurchaseOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrderController(ISender Sender) : ControllerBase
{
    [HttpGet("PendingReceipt")]
    public async Task<ApiResult<IEnumerable<PurchaseOrderVM>>> GetAllPO()
    {
        var result = await Sender.Send(new GetPurchaseOrdersQry());

        List<PurchaseOrderVM> ret = result.Adapt<List<PurchaseOrderVM>>();

        return ApiResult<IEnumerable<PurchaseOrderVM>>.Succeeded(ret);
    }

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<PurchaseOrderLineVM>>> PurchaseOrderItems(PurchaseOrderLineRequestDTO req)
    {
        var result = await Sender.Send(new GetPurchaseOrderLineQry(req));

        List<PurchaseOrderLineVM> ret = result.Adapt<List<PurchaseOrderLineVM>>();

        return ApiResult<IEnumerable<PurchaseOrderLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<ApiResult> PurchaseOrderSaveScan(List<PostPurchaseOrderDTO> req)
    {
        await Sender.Send(new PostPurchaseOrderCmd(req));

        return ApiResult.Succeeded();
    }
}
