using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Commands.Transaction.Packing.NS.VendorReturnAuthorization;
using Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;
using Application.UseCases.Commands.Transaction.TripTicket.NS;
using Application.UseCases.Queries.Transaction.TripTicket.NS;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.TripTicket;

namespace Api.CoreWebAPI.Controllers.TripTicket;

[ApiController]
[Route("api/[controller]")]
public class TripTicketController(ISender Sender) : ControllerBase
{
    [HttpGet("ItemFulfillment/Packed")]
    public async Task<ApiResult<IEnumerable<ItemFulfillmentVM>>> GetPackedItemFulfillment()
    {
        var result = await Sender.Send(new GetPackedItemFulfillmentsQry());

        List<ItemFulfillmentVM> ret = result.Adapt<List<ItemFulfillmentVM>>();

        return ApiResult<IEnumerable<ItemFulfillmentVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> TripTicketSaveScan(PostTripTicketDTO req)
    {
        ApiResult<bool> result = await Sender.Send(new PostTripTicketCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
