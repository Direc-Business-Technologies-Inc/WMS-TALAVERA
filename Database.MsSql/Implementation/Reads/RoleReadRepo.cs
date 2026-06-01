using Application.DataTransferObjects.Administration.Role;
using Application.DataTransferObjects.System.Modules;
using Application.UseCases.Repositories.Domain.Administration.User;
using Database.Libraries.Helpers;
using Database.MsSql.Core;
using Domain.Entities.Administration.User.Role;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;

namespace Database.MsSql.Implementation.Reads;

public class RoleReadRepo(IDbContextFactory<AppDbContext> dbContextFactory) : AppDbWork<RoleReadRepo>, IRoleReadRepo
{
    public Task<IEnumerable<ModulePermissionDTO>> GetRoleModulePermissions(Guid roleId)
    {
        return ExecuteAppDbWork<IEnumerable<ModulePermissionDTO>>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from u in ctx.Set<RoleDEM>().AsNoTracking()
                        from up in u.Permissions
                        join mp in ctx.Set<ModulePermissionDEM>().AsNoTracking() on up.ModulePermission equals mp.Id
                        join m in ctx.Set<ModuleDEM>().AsNoTracking() on mp.ModuleReference equals m.Id
                        select new ModulePermissionDTO
                        {
                            Id = mp.Id,
                            ModuleCode = m.Code,
                            Permission = mp.Permission.Value,
                        };

            var data = await query.ToListAsync();

            return data;
        });
    }

    public Task<IEnumerable<RolePermissionDTO>> GetRolePermissions(Guid roleId)
    {
        return ExecuteAppDbWork<IEnumerable<RolePermissionDTO>>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from r in ctx.Set<RoleDEM>().AsNoTracking()
                        from up in r.Permissions
                        join mp in ctx.Set<ModulePermissionDEM>().AsNoTracking() on up.ModulePermission equals mp.Id
                        join m in ctx.Set<ModuleDEM>().AsNoTracking() on mp.ModuleReference equals m.Id
                        where r.Id == roleId
                        select new RolePermissionDTO
                        {
                            Id = up.Id,
                            Permission = new ModulePermissionDTO
                            {
                                Id = mp.Id,
                                ModuleCode = m.Code,
                                Permission = mp.Permission.Value,
                            },
                            RoleId = r.Id
                        };

            var data = await query.ToListAsync();

            return data;
        });
    }

    public Task<(IEnumerable<RoleDTO> data, int count)> GetRoleTableDetails(DataGridIntent intent)
    {
        return ExecuteAppDbWork<(IEnumerable<RoleDTO>, int)>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = ctx.Set<RoleDEM>().AsNoTracking().Select(r => new RoleDTO
            {
                Id = r.Id,
                Active = r.Active,
                Code = r.Code,
                Name = r.Name,
            });

            var filterPredicate = LinqIntentExpressionBuilder.BuildPredicate<RoleDTO>(intent.Filters);
            int count = await query.CountAsync(filterPredicate);

            query = query.Where(filterPredicate);
            query = query.Where(x => x.Code != "ROOT");

            foreach (var sort in intent.Sorts)
                query = query.OrderByProperty(sort.Property, sort.Direction);

            query = query.Skip(intent.Skip);
            query = query.Take(intent.Take);

            List<RoleDTO> data = await query.ToListAsync();

            return (data, count);
        });
    }
}
