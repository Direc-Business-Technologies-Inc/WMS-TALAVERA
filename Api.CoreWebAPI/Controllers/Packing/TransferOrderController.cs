using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;
using Application.UseCases.Commands.Transaction.Receiving.NS.TransferOrder;
using Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.TransferOrder;

namespace Api.CoreWebAPI.Controllers.Packing;

[ApiController]
[Route("api/Packing/[controller]")]

public class TransferOrderController(ISender Sender) : ControllerBase
{
    [HttpPost("PendingFulfillment")]
    public async Task<ApiResult<IEnumerable<TransferOrderVM>>> GetAllTO(RequestPerUserDTO req)
    {
        var result = await Sender.Send(new GetTransferOrdersQry(req));

        List<TransferOrderVM> ret = result.Adapt<List<TransferOrderVM>>();

        return ApiResult<IEnumerable<TransferOrderVM>>.Succeeded(ret);
    }

    [HttpPost("Items")]
    public async Task<ApiResult<IEnumerable<TransferOrderLineVM>>> TransferOrderItems(TransferOrderLineRequestDTO req)
    {
        var result = await Sender.Send(new GetTransferOrderLineQry(req));

        List<TransferOrderLineVM> ret = result.Adapt<List<TransferOrderLineVM>>();

        return ApiResult<IEnumerable<TransferOrderLineVM>>.Succeeded(ret);
    }

    /// <summary>
    ///     Resolves the vendor barcode printed under the STR's Vendor field to the
    ///     vendor on that transfer order. The barcode encodes the vendor NAME, so
    ///     the lookup runs against the document's own line vendors.
    ///     <para>
    ///     Answers 400, never 404: <c>ApiClientService</c> turns a 404 into
    ///     "404. The endpoint does not exist.", which would hide the real reason.
    ///     </para>
    /// </summary>
    [HttpPost("ResolveVendor")]
    public async Task<IActionResult> ResolveVendor(ResolveVendorRequestDTO req)
    {
        var result = await Sender.Send(new ResolveTransferOrderVendorQry(req.OrderNumber, req.VendorBarcode));

        ApiResult<VendorLookupVM> response = result.Success && result.Data is not null
            ? ApiResult<VendorLookupVM>.Succeeded(result.Data.Adapt<VendorLookupVM>())
            : ApiResult<VendorLookupVM>.Failed(result.ErrorMessage);

        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("Vendors")]
    public async Task<IActionResult> GetVendors(GetTransferOrderVendorsRequestDTO req)
    {
        var result = await Sender.Send(new GetTransferOrderVendorsQry(req.OrderNumber));

        ApiResult<IEnumerable<VendorLookupVM>> response = result.Success && result.Data is not null
            ? ApiResult<IEnumerable<VendorLookupVM>>.Succeeded(result.Data.Adapt<List<VendorLookupVM>>())
            : ApiResult<IEnumerable<VendorLookupVM>>.Failed(result.ErrorMessage);

        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("SaveScan")]
    public async Task<IActionResult> TransferOrderSaveScan(List<PostTransferOrderDTO> req)
    {
        ApiResult<bool> result = await Sender.Send(new PostTransferOrderIFCmd(req));

        return StatusCode(result.StatusCode, result);
    }
}
