using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Packing.NS.Request;
using Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;
using Application.UseCases.Commands.Transaction.Packing.NS.VendorReturnAuthorization;
using Application.UseCases.Queries.Transaction.Packing.NS.VendorReturnAuthorization;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.VendorReturnAuthorization;

namespace Api.CoreWebAPI.Controllers.Packing;

[ApiController]
[Route("api/Packing/[controller]")]
public class VendorReturnAuthorizationController(ISender Sender) : ControllerBase
{
    [HttpPost("PendingReturn")]
    public async Task<ApiResult<IEnumerable<VendorReturnAuthorizationVM>>> GetAllVendorReturnAuthorization(RequestPerUserDTO req)
    {
        var result = await Sender.Send(new GetVendorReturnAuthorizationQry(req));

        List<VendorReturnAuthorizationVM> ret = result.Adapt<List<VendorReturnAuthorizationVM>>();

        return ApiResult<IEnumerable<VendorReturnAuthorizationVM>>.Succeeded(ret);
    }

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<VendorReturnAuthorizationLineVM>>> VendorReturnAuthorizationItems(VendorReturnAuthorizationLineRequestDTO req)
    {
        var result = await Sender.Send(new GetVendorReturnAuthorizationLineQry(req));

        List<VendorReturnAuthorizationLineVM> ret = result.Adapt<List<VendorReturnAuthorizationLineVM>>();

        return ApiResult<IEnumerable<VendorReturnAuthorizationLineVM>>.Succeeded(ret);
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> VendorReturnAuthorizationSaveScan(List<PostVendorReturnAuthorizationDTO> req)
    {
        ApiResult<bool> result = await Sender.Send(new PostVendorReturnAuthorizationIFCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
