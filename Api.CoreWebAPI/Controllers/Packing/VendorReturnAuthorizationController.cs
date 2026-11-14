using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Packing.NS.Request;
using Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;
using Application.UseCases.Commands.Transaction.Packing.NS.VendorReturnAuthorization;
using Application.UseCases.Queries.Transaction.Packing.NS.VendorReturnAuthorization;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.VendorReturnAuthorization;

namespace Api.CoreWebAPI.Controllers.Packing;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
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

    [HttpPost("Lookup")]
    public async Task<IActionResult> Lookup(RequestDTO req)
    {
        if (string.IsNullOrWhiteSpace(req.OrderNumber)) return StatusCode(400, ApiResult<VendorReturnAuthorizationVM>.Failed("Enter a vendor return authorization number."));
        if (!TryGetNetSuiteIdentity(out var user)) return Unauthorized(ApiResult<VendorReturnAuthorizationVM>.Failed("Authenticated NetSuite employee identity is unavailable."));
        var result = await Sender.Send(new GetPackingVendorReturnByNumberQry(req.OrderNumber, user));
        if (result is null) return Ok(ApiResult<VendorReturnAuthorizationVM>.NotFound("No eligible vendor return authorization was found."));
        return Ok(ApiResult<VendorReturnAuthorizationVM>.Succeeded(result.Adapt<VendorReturnAuthorizationVM>()));
    }

    private bool TryGetNetSuiteIdentity(out RequestPerUserDTO user)
    {
        var employee = User.FindFirst("NsEmployeeId")?.Value;
        var subsidiary = User.FindFirst("NsSubsidiaryId")?.Value;
        user = new RequestPerUserDTO();
        if (!int.TryParse(employee, out var employeeId) || employeeId <= 0 || !int.TryParse(subsidiary, out var subsidiaryId) || subsidiaryId <= 0) return false;
        user.NetsuiteUserInternalId = employeeId;
        user.NetsuiteUserSubsidiaryInternalId = subsidiaryId;
        return true;
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
