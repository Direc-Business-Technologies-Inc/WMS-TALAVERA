using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Shared.Libraries.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class LocationIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor accessor,
    SuiteQLQueryBuilderFactoryService builderFactory) : ILocationIntegration
{
    public async Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("loc.id", nameof(LocationDTO.Id)),
                ("loc.externalId", nameof(LocationDTO.LocationNumber)),
                ("loc.name", nameof(LocationDTO.Name)),
                ("BUILTIN.DF(loc.mainaddress)", nameof(LocationDTO.Address)),
                ("BUILTIN.DF(loc.subsidiary)", nameof(LocationDTO.Subsidiary)),
                ("loc.subsidiary", nameof(LocationDTO.SubsidiaryId)),
                ("(SELECT COUNT(1) FROM bin b WHERE b.location = loc.id)", nameof(LocationDTO.BinsCount))
            )
            .From("location loc")
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<LocationDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }
    public async Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiary)
    {
        var query = builderFactory.Create()
            .Select(
                ("loc.id", nameof(LocationDTO.Id)),
                ("loc.externalId", nameof(LocationDTO.LocationNumber)),
                ("loc.name", nameof(LocationDTO.Name)),
                ("BUILTIN.DF(loc.mainaddress)", nameof(LocationDTO.Address)),
                ("BUILTIN.DF(loc.subsidiary)", nameof(LocationDTO.Subsidiary)),
                ("loc.subsidiary", nameof(LocationDTO.SubsidiaryId))
            )
            .From("location loc")
            .Join("LocationSubsidiaryMap lsm", on: "lsm.location = loc.id")
            .WithFilter(DataGridFilterUtilities.Equal("lsm.subsidiary", subsidiary))
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<LocationDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }
    public async Task<(IEnumerable<LocationDTO> data, int count)> GetSublocationsOfLocationAsync(DataGridIntent intent, int location)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(LocationDTO.Id)),
                ("externalId", nameof(LocationDTO.LocationNumber)),
                ("name", nameof(LocationDTO.Name)),
                ("BUILTIN.DF(mainaddress)", nameof(LocationDTO.Address)),
                ("BUILTIN.DF(subsidiary)", nameof(LocationDTO.Subsidiary)),
                ("subsidiary", nameof(LocationDTO.SubsidiaryId))
            )
            .From("location")
            .WithFilter(
                DataGridFilterUtilities.Equal("parent", location)
            )
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<LocationDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }
    public async Task<(IEnumerable<LocationDTO> data, int count)> GetCurrentUserAllowedLocations(DataGridIntent intent)
    {
        string? claimValue = accessor.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsEmployeeId")?.Value;
        if (claimValue is null) return ([], 0);
        int employeeId;
        try
        {
            employeeId = int.Parse(claimValue);
        }
        catch 
        {
            return ([], 0);
        }


        var query = builderFactory.Create()
            .Select(
                ("loc.id", nameof(LocationDTO.Id)),
                ("loc.externalId", nameof(LocationDTO.LocationNumber)),
                ("loc.name", nameof(LocationDTO.Name)),
                ("loc.BUILTIN.DF(mainaddress)", nameof(LocationDTO.Address)),
                ("loc.BUILTIN.DF(subsidiary)", nameof(LocationDTO.Subsidiary)),
                ("loc.subsidiary", nameof(LocationDTO.SubsidiaryId))
            )
            .From("employee e")
            .Join("MAP_employee_custentity_dbti_wms_user_location_access map", "map.mapone = e.id")
            .Join("location loc", "map.maptwo = loc.id")
            .WithFilter(
                DataGridFilterUtilities.Equal("e.id", employeeId)
            )
            .WithDatagridIntent(intent)
            .Build();

        var result = await query.ExecuteWithPaging<LocationDTO>(netsuiteService);
        return (result.items, result.totalResults);
    }

    public async Task<LocationDTO?> GetLocation(int locationId)
    {
        var query = builderFactory.Create()
            .Select(
                ("loc.id", nameof(LocationDTO.Id)),
                ("loc.externalId", nameof(LocationDTO.LocationNumber)),
                ("loc.name", nameof(LocationDTO.Name)),
                ("BUILTIN.DF(loc.mainaddress)", nameof(LocationDTO.Address)),
                ("BUILTIN.DF(loc.subsidiary)", nameof(LocationDTO.Subsidiary)),
                ("loc.subsidiary", nameof(LocationDTO.SubsidiaryId)),
                ("(SELECT COUNT(1) FROM bin b WHERE b.location = loc.id)", nameof(LocationDTO.BinsCount))
            )
            .From("location loc")
            .WithFilter(
                DataGridFilterUtilities.Equal("id", locationId)
            )
            .Build();
        var result = await query.ExecuteWithPaging<LocationDTO>(netsuiteService);
        return result.items.FirstOrDefault();
    }

    public async Task<(IEnumerable<LocationBinDTO> data, int count)> GetLocationBinsAsync(LocationDTO dto, DataGridIntent intent)
    {
        return await GetLocationBinsAsync(dto.Id, intent);
    }

    public async Task<LocationDTO?> GetParentLocation(int locationId)
    {
        var query = builderFactory.Create()
            .Select(
                ("ploc.id", nameof(LocationDTO.Id)),
                ("ploc.externalId", nameof(LocationDTO.LocationNumber)),
                ("ploc.name", nameof(LocationDTO.Name)),
                ("BUILTIN.DF(ploc.mainaddress)", nameof(LocationDTO.Address)),
                ("BUILTIN.DF(ploc.subsidiary)", nameof(LocationDTO.Subsidiary)),
                ("ploc.subsidiary", nameof(LocationDTO.SubsidiaryId))
            )
            .From("location ploc")
            .Join("location cloc", on: "cloc.parent = ploc.id")
            .WithFilter(
                DataGridFilterUtilities.Equal("cloc.id", locationId)
            )
            .Build();

        var result = await query.ExecuteWithPaging<LocationDTO>(netsuiteService);
        return result.items.FirstOrDefault();
    }

    public async Task<(IEnumerable<LocationBinDTO> data, int count)> GetLocationBinsAsync(int locationId, DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("bin.id", nameof(LocationBinDTO.Id)),
                ("bin.binnumber", nameof(LocationBinDTO.BinNumber)),
                ("bin.memo", nameof(LocationBinDTO.Memo))
            )
            .From("bin")
            .WithFilters(DataGridFilterUtilities.Equal("bin.location", locationId))
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<LocationBinDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.totalResults);
    }
}
