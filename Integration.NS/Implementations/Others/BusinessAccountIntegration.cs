using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class BusinessAccountIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService factoryService) : IBusinessAccountIntegration
{
    public async Task<(IEnumerable<BusinessAccountDTO> data, int count)> GetBusinessAccountsAsync(DataGridIntent intent, int? subsidiary = null)
    {
        var builder = factoryService.Create()
            .Select(
                ("acc.acctnumber", nameof(BusinessAccountDTO.AccountNumber)),
                ("acc.id", nameof(BusinessAccountDTO.Id)),
                ("acc.fullname", nameof(BusinessAccountDTO.Name)),
                ("BUILTIN.DF(acc.accttype)", nameof(BusinessAccountDTO.AccountType))
            )
            .From("account acc")
            .WithDatagridIntent(intent);

        if (subsidiary != null)
        {
            builder.Join("accountsubsidiarymap asm", on: "asm.account = acc.id")
                .WithFilters(DataGridFilterUtilities.Equal("asm.subsidiary", subsidiary ?? 0));
        }

        var query = builder.Build();
        var response = await query.ExecuteWithPaging<BusinessAccountDTO>(netsuiteService);

        return (response.items, response.totalResults);
    }
}