using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Queries.Others.NS;
using Application.UseCases.Queries.Transaction.InventoryCounting.NS;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Common;

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

    [HttpPost("PerBarcode")]
    public async Task<ApiResult<InventoryItemVM>> ItemPerBarcode(ItemBarcodeDTO req)
    {
        var result = await Sender.Send(new GetItemPerBarcodeQry(req.Barcode));

        InventoryItemVM ret = result.Adapt<InventoryItemVM>();

        return ApiResult<InventoryItemVM>.Succeeded(ret);
    }
}
