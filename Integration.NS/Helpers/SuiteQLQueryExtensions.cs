using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Integration.NS.Helpers;

public static class SuiteQLQueryExtensions
{
    /// <summary>
    ///     SuiteQL requires the offset to be a multiple of the page size
    ///     however, with radzen dropdowns, this is not always the case.
    ///     to work around this, this extension function loads the page starting from
    ///     the lowest multiple of pagesize and then the page after that.
    ///     this then stitches the pages together and removes the unneeded values
    /// </summary>
    /// <typeparam name="T">Type to cast the netsuite response to</typeparam>
    /// <param name="query">SuiteQLQuery object containing the query</param>
    /// <param name="netsuiteService">INetSuiteApiClientService to send the request to</param>
    /// <returns></returns>
    public static async Task<NetSuiteResponse<T>> ExecuteWithPaging<T>(this SuiteQLQuery query, INetSuiteApiClientService netsuiteService)
    {
        if (query.Limit < 0) { query.Limit = null; }
        if (query.Offset is null || query.Limit is null)
        {
            return await netsuiteService.ExecuteSuiteQLQuery<T>(query.Query, query.Limit, query.Offset);
        }

        int modulos = (int)query.Offset % (int)query.Limit;
        if (modulos == 0)
        {
            return await netsuiteService.ExecuteSuiteQLQuery<T>(query.Query, query.Limit, query.Offset);
        }

        int nearestMultiple = (int)query.Offset - modulos;

        // query values one at a time to try and avoid hitting netsuite's concurrency limit
        var response0 = await netsuiteService.ExecuteSuiteQLQuery<T>(query.Query, query.Limit, nearestMultiple);
        var response1 = await netsuiteService.ExecuteSuiteQLQuery<T>(query.Query, query.Limit, nearestMultiple + query.Limit);

        List<T> itemsStitched = [
           .. response0.items.Skip(modulos),
            .. response1.items.Take(modulos)
           ];

        // use response1 so we dont have to create a new object
        response1.items = itemsStitched;
        response1.count = itemsStitched.Count;
        return response1;
    }

    /// <summary>
    ///     Very hacky and janky but reduces reusing of code 
    ///     basically just filters the transaction given by transactionTablename
    ///     so that the transaction's subsidiary fields are in the current users
    ///     allowed subsidiary unless destination=true in which case it filters 
    ///     the transactions tosubsidiary
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="context"></param>
    /// <param name="transactionTablename"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public static SuiteQLQueryBuilder WithSubsidiaries(this SuiteQLQueryBuilder builder, IHttpContextAccessor context, string transactionTablename, bool destination = false)
    {
        if (string.IsNullOrWhiteSpace(transactionTablename)) return builder;
        string? claimValue = context.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedSubsidiaries")?.Value;
        if (claimValue == null) return builder;

        List<int> allowedSubsidiaries = JsonSerializer.Deserialize<List<int>>(claimValue) ?? [];

        if (allowedSubsidiaries.Count == 0) return builder;
        return destination ? builder.WithFilter(DataGridFilterUtilities.In($"{transactionTablename}.tosubsidiary", allowedSubsidiaries))
            : builder.WithFilter(DataGridFilterUtilities.In($"{transactionTablename}.subsidiary", allowedSubsidiaries));
    }
}