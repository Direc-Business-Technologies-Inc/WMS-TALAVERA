using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Commands.Transaction.TripTicket.NS;
using Application.UseCases.Queries.Transaction.TripTicket.NS;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.ItemFulfillment;

namespace Api.CoreWebAPI.Controllers.TripTicket;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("api/[controller]")]
public class TripTicketController(ISender Sender) : ControllerBase
{
    [HttpGet("ItemFulfillment/Packed")]
    public async Task<IActionResult> GetPackedItemFulfillment()
    {
        if (!int.TryParse(User.FindFirst("NsSubsidiaryId")?.Value, out var subsidiaryId) || subsidiaryId <= 0)
            return Unauthorized(ApiResult<IEnumerable<ItemFulfillmentVM>>.Failed("Authenticated NetSuite subsidiary identity is unavailable."));

        var result = await Sender.Send(new GetPackedItemFulfillmentsQry(subsidiaryId));

        List<ItemFulfillmentVM> ret = result.Adapt<List<ItemFulfillmentVM>>();

        return Ok(ApiResult<IEnumerable<ItemFulfillmentVM>>.Succeeded(ret));
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> TripTicketSaveScan(PostTripTicketDTO req)
    {
        ApiResult<bool> result = await Sender.Send(new PostTripTicketCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
