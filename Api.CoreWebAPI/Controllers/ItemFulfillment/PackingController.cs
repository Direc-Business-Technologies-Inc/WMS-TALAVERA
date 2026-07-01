using Application.DataTransferObjects.Transactions.ItemFulfillment.NS.Request;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Commands.Transaction.Receiving.NS.Returns;
using Application.UseCases.Queries.Transaction.Receiving.NS.Packing;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers.ItemFulfillment
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackingController(ISender Sender) : ControllerBase
    {
        [HttpGet("PendingFulfillment")]
        public async Task<ApiResult<IEnumerable<PackingVM>>> GetAllPacking()
        {
            var result = await Sender.Send(new GetPackingQry());

            List<PackingVM> ret = result.Adapt<List<PackingVM>>();

            return ApiResult<IEnumerable<PackingVM>>.Succeeded(ret);
        }

        [HttpPost("Items")]
        public async Task<ApiResult<IEnumerable<PackingLineVM>>> PackingItems(PackingLineRequestDTO req)
        {
            var result = await Sender.Send(new GetPackingLineQry(req));

            List<PackingLineVM> ret = result.Adapt<List<PackingLineVM>>();

            return ApiResult<IEnumerable<PackingLineVM>>.Succeeded(ret);
        }

        [HttpPost("SaveScan")]
        public async Task<ApiResult> ReturnsSaveScan(List<PostReturnsDTO> req)
        {
            await Sender.Send(new PostReturnsCmd(req));

            return ApiResult.Succeeded();
        }
    }
}
