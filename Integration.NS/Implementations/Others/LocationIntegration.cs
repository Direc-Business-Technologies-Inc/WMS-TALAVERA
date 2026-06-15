using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Services;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class LocationIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : ILocationIntegration
{
    public async Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsAsync(DataGridIntent intent)
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
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<LocationDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.totalResults);
    }
}
