using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Queries.Others.NS;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;

namespace Api.CoreWebAPI.Controllers.Lookup;


[ApiController]
[Route("api/[controller]")]
public class LookupController(ISender Sender) : ControllerBase
{
    [HttpGet("Drivers")]
    public async Task<ApiResult<IEnumerable<DriverVM>>> GetDrivers()
    {
        var result = await Sender.Send(new GetDriversQry());

        List<DriverVM> ret = result.Adapt<List<DriverVM>>();

        return ApiResult<IEnumerable<DriverVM>>.Succeeded(ret);
    }

    [HttpGet("Locations")]
    public async Task<ApiResult<IEnumerable<LocationVM>>> GetLocations()
    {
        var result = await Sender.Send(new GetLocationsQry());

        List<LocationVM> ret = result.Adapt<List<LocationVM>>();

        return ApiResult<IEnumerable<LocationVM>>.Succeeded(ret);
    }

    [HttpPost("Susidiary/Locations")]
    public async Task<ApiResult<IEnumerable<LocationVM>>> GetSubsidiaryLocations(RequestPerSubsidiaryDTO req)
    {
        var result = await Sender.Send(new GetSubsidiariesLocationQry(req));
        var ret = result.Adapt<List<LocationVM>>();
        return ApiResult<IEnumerable<LocationVM>>.Succeeded(ret);
    }

    [HttpGet("Helpers")]
    public async Task<ApiResult<IEnumerable<HelperVM>>> GetHelpers()
    {
        var result = await Sender.Send(new GetHelpersQry());

        List<HelperVM> ret = result.Adapt<List<HelperVM>>();

        return ApiResult<IEnumerable<HelperVM>>.Succeeded(ret);
    }

    [HttpGet("TruckPlateNumbers")]
    public async Task<ApiResult<IEnumerable<TruckPlateNumberVM>>> GetTruckPlateNo()
    {
        var result = await Sender.Send(new GetTruckPlateNumbersQry());

        List<TruckPlateNumberVM> ret = result.Adapt<List<TruckPlateNumberVM>>();

        return ApiResult<IEnumerable<TruckPlateNumberVM>>.Succeeded(ret);
    }

    [HttpPost("BinLocations")]
    public async Task<ApiResult<IEnumerable<BinVM>>> GetBinLocations(BinLocationRequestDTO req)
    {
        var result = await Sender.Send(new GetBinsPerLocationQry(req));
        var ret = result.Adapt<List<BinVM>>();
        return ApiResult<IEnumerable<BinVM>>.Succeeded(ret);
    }
}
