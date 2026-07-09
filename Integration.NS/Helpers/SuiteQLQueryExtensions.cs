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

        var responses = await Task.WhenAll(
                netsuiteService.ExecuteSuiteQLQuery<T>(query.Query, query.Limit, nearestMultiple),
                netsuiteService.ExecuteSuiteQLQuery<T>(query.Query, query.Limit, nearestMultiple + query.Limit)
            );

        List<T> itemsStitched = [
           .. responses[0].items.Skip(modulos),
            .. responses[1].items.Take(modulos)
           ];

        responses[0].items = itemsStitched;
        responses[0].count = itemsStitched.Count;
        return responses[0];
    }

    public static SuiteQLQueryBuilder WithSubsidiaries(this SuiteQLQueryBuilder builder, IHttpContextAccessor context, string transactionTablename)
    {
        if (string.IsNullOrWhiteSpace(transactionTablename)) return builder;
        string? claimValue = context.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsAllowedSubsidiaries")?.Value;
        if (claimValue == null) return builder;

        List<int> allowedSubsidiaries = JsonSerializer.Deserialize<List<int>>(claimValue) ?? [];

        if (allowedSubsidiaries.Count == 0) return builder;
        return builder.WithFilter(
            DataGridFilterUtilities.In($"{transactionTablename}.subsidiary", allowedSubsidiaries));
    }
}