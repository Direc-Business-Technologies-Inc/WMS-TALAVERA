using Application.DataTransferObjects.System.Commons;
using Application.UseCases.Repositories.Domain.System;
using Database.MsSql.Core;
using Domain.Entities.System;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Database.MsSql.Implementation.Reads;

public class NavigationRouteReadRepo(IDbContextFactory<AppDbContext> dbContextFactory) : AppDbWork<NavigationRouteReadRepo>, INavigationRouteReadRepo
{
    public Task<IEnumerable<NavigationRouteDTO>> GetModuleNavigationRoute(string moduleCode)
    {
        return ExecuteAppDbWork<IEnumerable<NavigationRouteDTO>>(async () =>
        {

            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var rawResults = await ctx.Database
                .SqlQueryRaw<NavigationRouteDTO>(@"
                    WITH RouteHierarchy AS (
                        SELECT 
                            nr.Id,
                            nr.Name,
                            nr.Protected,
                            nr.Position,
                            nr.Icon,
                            nr.Uri,
                            nr.ParentId,
                            nr.Active,
                            0 AS Depth
                        FROM ONRT nr
                        INNER JOIN OMDL m ON m.NavRouteId = nr.Id
                        WHERE m.Code = {0} AND nr.Active = 1
                
                        UNION ALL
                
                        SELECT 
                            parent.Id,
                            parent.Name,
                            parent.Protected,
                            parent.Position,
                            parent.Icon,
                            parent.Uri,
                            parent.ParentId,
                            parent.Active,
                            child.Depth + 1 AS Depth
                        FROM ONRT parent
                        INNER JOIN RouteHierarchy child ON child.ParentId = parent.Id
                        WHERE parent.Active = 1
                    )
                    SELECT 
                        Id, Name, Protected, Position, Icon, Uri, ParentId, Active
                    FROM RouteHierarchy
                    ORDER BY Depth DESC", moduleCode)
                .ToListAsync();

            var result = rawResults.Select(r => new NavigationRouteDTO
            {
                Id = r.Id,
                Name = r.Name,
                Protected = r.Protected,
                Position = r.Position,
                Icon = r.Icon,
                Uri = r.Uri,
                ParentId = r.ParentId,
                Active = r.Active
            });

            if (!result.Any(r => r.Name.Equals("Dashboard")))
            {
                var query = ctx.Set<NavigationRouteDEM>().FirstOrDefaultAsync(nr => nr.Name == "Dashboard");

                NavigationRouteDEM? dashboard = await query;
                if (dashboard is not null)
                    result = [dashboard.Adapt<NavigationRouteDTO>(), .. result];
            }

            return result;
        });
    }
}

public class NavigationRouteWithDepth : NavigationRouteDEM
{
    public int Depth { get; set; }
}