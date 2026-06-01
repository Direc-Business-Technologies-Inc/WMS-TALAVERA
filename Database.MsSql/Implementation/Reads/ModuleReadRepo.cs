using Application.DataTransferObjects.System.Modules;
using Application.UseCases.Repositories.Domain.System;
using Database.Libraries.Helpers;
using Database.MsSql.Core;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;

namespace Database.MsSql.Implementation.Reads;

public class ModuleReadRepo(IDbContextFactory<AppDbContext> dbContextFactory) : AppDbWork<ModuleReadRepo>, IModuleReadRepo
{
    public Task<(IEnumerable<ModuleDataGridDTO> Data, int Count)> GetModuleTableDetails(DataGridIntent intent)
    {
        return ExecuteAppDbWork<(IEnumerable<ModuleDataGridDTO>, int)>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from m in ctx.Set<ModuleDEM>().AsNoTracking()
                        select new ModuleDataGridDTO
                        {
                            Id = m.Id,
                            Name = m.Name,
                            Active = m.Active,
                            Code = m.Code,
                            Root = m.Root,
                            Transactional = m.Transactional
                        };

            var filterPredicate = LinqIntentExpressionBuilder.BuildPredicate<ModuleDataGridDTO>(intent.Filters);
            int count = await query.CountAsync(filterPredicate);

            query = query.Where(filterPredicate);

            foreach (var sort in intent.Sorts)
                query = query.OrderByProperty(sort.Property, sort.Direction);

            query = query.Skip(intent.Skip);
            query = query.Take(intent.Take);

            List<ModuleDataGridDTO> data = await query.ToListAsync();

            var query2 = from mp in ctx.Set<ModulePermissionDEM>().AsNoTracking()
                         select mp;

            List<ModulePermissionDEM> modulePermissions = await query2.ToListAsync();

            foreach (var module in data)
            {
                module.Permissions = [.. modulePermissions.Where(mp => mp.ModuleReference == module.Id).Select(mp => new ModulePermissionDTO()
                {
                    Id = mp.Id,
                    ModuleId = mp.Id,
                    ModuleCode = module.Code,
                    Permission = mp.Permission.Value
                })];
            }

            return (data, count);
        });
    }
}
