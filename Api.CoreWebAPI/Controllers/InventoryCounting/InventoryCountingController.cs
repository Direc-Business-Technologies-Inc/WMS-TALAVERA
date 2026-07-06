using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS.Request;
using Application.UseCases.Commands.Transaction.InventoryCounting.NS;
using Application.UseCases.Queries.Transaction.InventoryCounting.NS;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.Common;
using Shared.Libraries.ViewModel.InventoryCounting;

namespace Api.CoreWebAPI.Controllers.InventoryCounting;

[ApiController]
[Route("api/[controller]")]
public class InventoryCountingController(ISender Sender) : ControllerBase
{
    [HttpPost("Started")]
    public async Task<ApiResult<IEnumerable<InventoryCountingVM>>> GetAllStartedInventoryCount(RequestPerSubsidiaryDTO req)
    {
        var result = await Sender.Send(new GetStartedInventoryCountingQry(req));

        List<InventoryCountingVM> ret = result.Adapt<List<InventoryCountingVM>>();

        return ApiResult<IEnumerable<InventoryCountingVM>>.Succeeded(ret);
    }

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<InventoryCountingLineVM>>> InventoryCountItems(InventoryCountingLineRequestDTO req)
    {
        var result = await Sender.Send(new GetStartedInventoryCountingLineQry(req));

        List<InventoryCountingLineVM> ret = result.Adapt<List<InventoryCountingLineVM>>();

        return ApiResult<IEnumerable<InventoryCountingLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> InventoryCountSaveScan(List<PatchInventoryCountingDTO> req)
    {
        ApiResult<bool> result = await Sender.Send(new PatchInventoryCountingCmd(req));

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("Worksheet/Items")]
    public async Task<ApiResult<IEnumerable<InventoryItemVM>>> GetAllWorksheetItems()
    {
        var result = await Sender.Send(new GetInventoryItemsQry());

        List<InventoryItemVM> ret = result.Adapt<List<InventoryItemVM>>();

        return ApiResult<IEnumerable<InventoryItemVM>>.Succeeded(ret);
    }

    [HttpPost("Worksheet/SaveScan")]
    public async Task<IActionResult> InventoryWorksheetSaveScan(SaveInventoryWorksheetRequestDTO req)
    {
        ApiResult<bool> result = await Sender.Send(new PostInventoryWorksheetCmd(req.InventoryCountItems, req.Location, req.NetsuiteUserSubsidiaryInternalId));

        return StatusCode(result.StatusCode, result);
    }
}