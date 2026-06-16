using Application.DataTransferObjects.Others.NS;
using Application.UseCases.Repositories.Integration.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Helpers;

public static class SuiteQLQueryExtensions
{
    public static async Task<NetSuiteResponse<T>> ExecuteWithPaging<T>(this SuiteQLQuery query, INetSuiteApiClientService netsuiteService)
    {
        int? origLimit = null;
        int? origOffset = null;
        int? limit = query.Limit;
        int? offset = query.Limit;
        if (offset != null && limit != null)
        {
            if (query.Offset < 0) throw new InvalidOperationException("Cannot offset by a negative number");
            int remainder = (int)(offset % limit);
            if (remainder != 0)
            {
                origLimit = query.Limit;
                origOffset = query.Offset;
                offset = offset - remainder;
                limit *= 2;
            }
        }

        var response = await netsuiteService.ExecuteSuiteQLQuery<T>(query.Query, limit, offset);
        if (origLimit is null || origOffset is null) return response;

        response.items = [.. response.items.Skip((int)origOffset - (offset ?? 0)).Take((int)origLimit)];
        response.count = response.items.Count;
        return response;
    }
}