using Application.DataTransferObjects.Transactions.Receiving.Request;
using Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;
using Application.UseCases.Queries.Transaction.Receiving.NS.PurchaseOrder;
using Application.UseCases.Queries.Transaction.Receiving.NS.TransferOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers.Receiving;

[ApiController]
[Route("api/[controller]")]
//[Authorize(AuthenticationSchemes = "Bearer")]
public class ReceivingController(ISender Sender) : ControllerBase
{
    [HttpGet("PO/PendingReceipt")]
    public async Task<ApiResult<IEnumerable<PurchaseOrderVM>>> GetAllPO()
    {
        var result = await Sender.Send(new GetPurchaseOrdersQry());

        List<PurchaseOrderVM> ret = result.Adapt<List<PurchaseOrderVM>>();

        return ApiResult<IEnumerable<PurchaseOrderVM>>.Succeeded(ret);
    }

    [HttpPost("PO/Items")]
    public async Task<ApiResult<IEnumerable<PurchaseOrderLineVM>>> PurchaseOrderItems(PurchaseOrderLineRequestDTO req)
    {
        var result = await Sender.Send(new GetPurchaseOrderLineQry(req));

        List<PurchaseOrderLineVM> ret = result.Adapt<List<PurchaseOrderLineVM>>();

        return ApiResult<IEnumerable<PurchaseOrderLineVM>>.Succeeded(ret);
    }

    [HttpPost("PO/SaveScan/Good")]
    public async Task<ApiResult> PurchaseOrderSaveScan(List<PurchaseOrderLineVM> req)
    {
        await Sender.Send(new PostPurchaseOrderCmd(req));

        return ApiResult.Succeeded();
    }

    [HttpGet("TO/PendingReceipt")]
    public async Task<ApiResult<IEnumerable<TransferOrderVM>>> GetAllTO()
    {
        var result = await Sender.Send(new GetTransferOrdersQry());

        List<TransferOrderVM> ret = result.Adapt<List<TransferOrderVM>>();

        return ApiResult<IEnumerable<TransferOrderVM>>.Succeeded(ret);
    }

    [HttpPost("TO/Items")]
    public async Task<ApiResult<IEnumerable<TransferOrderLineVM>>> TransferOrderItems(TransferOrderLineRequestDTO req)
    {
        var result = await Sender.Send(new GetTransferOrderLineQry(req));

        List<TransferOrderLineVM> ret = result.Adapt<List<TransferOrderLineVM>>();

        return ApiResult<IEnumerable<TransferOrderLineVM>>.Succeeded(ret);
    }
}
