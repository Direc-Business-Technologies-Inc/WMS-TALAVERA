using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Queries.Others.NS;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers.Item;

[ApiController]
[Route("api/[controller]")]
//[Authorize(AuthenticationSchemes = "Bearer")]
public class ItemController(ISender Sender) : Controller
{
    [HttpPost("Barcodes")]
    public async Task<ApiResult<IEnumerable<ItemBarcodesPerUoMVM>>> ItemBarcodes(List<ItemBarcodesRequestDTO> req)
    {
        var result = await Sender.Send(new GetItemBarcodesPerUoMQry(req));

        List<ItemBarcodesPerUoMVM> ret = result.Adapt<List<ItemBarcodesPerUoMVM>>();

        return ApiResult<IEnumerable<ItemBarcodesPerUoMVM>>.Succeeded(ret);
    }
}
