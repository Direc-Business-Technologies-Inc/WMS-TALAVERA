using Api.CoreWebAPI.Controllers.Authentication.Repositories;
using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Queries.Transaction.Receiving.PurchaseOrder.Mobile;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(AuthenticationSchemes = "Bearer")]
    public class ReceivingController(ISender Sender) : ControllerBase
    {
        [HttpGet("PO/PendingReceipt")]
        public async Task<ApiResult<IEnumerable<PurchaseOrderVM>>> GetAll()
        {
            var result = await Sender.Send(new MGetPurchaseOrdersQry());

            List<PurchaseOrderVM> ret = result.Adapt<List<PurchaseOrderVM>>();

            return ApiResult<IEnumerable<PurchaseOrderVM>>.Succeeded(ret);
        }
    }
}
