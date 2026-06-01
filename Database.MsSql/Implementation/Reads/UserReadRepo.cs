using Application.DataTransferObjects.Administration.User;
using Application.DataTransferObjects.System.Commons;
using Application.DataTransferObjects.System.Modules;
using Application.UseCases.Repositories.Domain.Administration.User;
using Database.Libraries.Helpers;
using Database.MsSql.Core;
using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;

namespace Database.MsSql.Implementation.Reads;

public class UserReadRepo(IDbContextFactory<AppDbContext> dbContextFactory) : AppDbWork<UserReadRepo>, IUserReadRepo
{
    public Task<UserDataGridDTO?> GetDriverDetails(Guid driverId)
    {
        return ExecuteAppDbWork<UserDataGridDTO?>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from u in ctx.Set<UserDEM>().AsNoTracking()
                        join r in ctx.Set<RoleDEM>().AsNoTracking() on u.RoleId equals r.Id
                        where r.Code == "LGS_DRIVER" && u.Id == driverId
                        select new UserDataGridDTO
                        {
                            Id = u.Id,
                            FullName = u.Name.FullName,
                            UserName = u.Account.UserName.Value,
                            Email = u.Email.Address,
                            Phone = u.PhoneNumber,
                            Active = u.Active,
                            Position = r.Name
                        };

            UserDataGridDTO? data = await query.FirstOrDefaultAsync();

            return data;
        });
    }

    public Task<IEnumerable<UserDataGridDTO>> GetDriverUsers()
    {
        return ExecuteAppDbWork<IEnumerable<UserDataGridDTO>>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from u in ctx.Set<UserDEM>().AsNoTracking()
                        join r in ctx.Set<RoleDEM>().AsNoTracking() on u.RoleId equals r.Id
                        where r.Code == "LGS_DRIVER"
                        select new UserDataGridDTO
                        {
                            Id = u.Id,
                            FullName = u.Name.FullName,
                            UserName = u.Account.UserName.Value,
                            Email = u.Email.Address,
                            Phone = u.PhoneNumber,
                            Active = u.Active,
                            Position = r.Name
                        };

            List<UserDataGridDTO> data = await query.ToListAsync();

            return data;
        });
    }

    public Task<IEnumerable<ModulePermissionDTO>> GetUserModulePermissions(Guid userId)
    {
        return ExecuteAppDbWork<IEnumerable<ModulePermissionDTO>>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from u in ctx.Set<UserDEM>().AsNoTracking()
                        from up in u.Permissions
                        join mp in ctx.Set<ModulePermissionDEM>().AsNoTracking() on up.ModulePermission equals mp.Id
                        join m in ctx.Set<ModuleDEM>().AsNoTracking() on mp.ModuleReference equals m.Id
                        where u.Id == userId
                        select new ModulePermissionDTO
                        {
                            ModuleCode = m.Code,
                            Permission = mp.Permission.Value,
                        };

            var data = await query.ToListAsync();

            return data;
        });
    }

    public Task<IEnumerable<UserPermissionDTO>> GetUserPermissions(Guid userId)
    {
        return ExecuteAppDbWork<IEnumerable<UserPermissionDTO>>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from u in ctx.Set<UserDEM>().AsNoTracking()
                        from up in u.Permissions
                        join mp in ctx.Set<ModulePermissionDEM>().AsNoTracking() on up.ModulePermission equals mp.Id
                        join m in ctx.Set<ModuleDEM>().AsNoTracking() on mp.ModuleReference equals m.Id
                        where u.Id == userId
                        select new UserPermissionDTO
                        {
                            Id = up.Id,
                            Permission = new ModulePermissionDTO
                            {
                                Id = mp.Id,
                                ModuleCode = m.Code,
                                Permission = mp.Permission.Value,
                            },
                            UserId = u.Id
                        };

            var data = await query.ToListAsync();

            return data;
        });
    }

    public Task<IEnumerable<NavigationRouteDTO>> GetUserRoutes(Guid userId)
    {
        return ExecuteAppDbWork<IEnumerable<NavigationRouteDTO>>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from u in ctx.Set<UserDEM>().AsNoTracking()
                        from up in u.Permissions
                        join mp in ctx.Set<ModulePermissionDEM>().AsNoTracking() on up.ModulePermission equals mp.Id
                        join m in ctx.Set<ModuleDEM>().AsNoTracking() on mp.ModuleReference equals m.Id
                        join nr in ctx.Set<NavigationRouteDEM>().AsNoTracking() on m.NavRouteId equals nr.Id
                        where u.Id == userId && nr.Active && nr.Protected == true
                        select new NavigationRouteDTO
                        {
                            Id = nr.Id,
                            Name = nr.Name,
                            Position = nr.Position,
                            Icon = nr.Icon,
                            Uri = nr.Uri,
                            ParentId = nr.ParentId,
                            Active = nr.Active
                        };

            var data = await query.Distinct().ToListAsync();

            return data.ToHashSet();
        });
    }

    public Task<(IEnumerable<UserDataGridDTO> data, int count)> GetUserTableDetails(DataGridIntent intent)
    {
        return ExecuteAppDbWork<(IEnumerable<UserDataGridDTO>, int)>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            var query = from u in ctx.Set<UserDEM>().AsNoTracking()
                        join r in ctx.Set<RoleDEM>().AsNoTracking() on u.RoleId equals r.Id
                        select new UserDataGridDTO
                        {
                            Id = u.Id,
                            FirstName = u.Name.FirstName,
                            LastName = u.Name.LastName,
                            FullName = u.Name.FullName,
                            UserName = u.Account.UserName.Value,
                            Email = u.Email.Address,
                            Phone = u.PhoneNumber,
                            Active = u.Active,
                            Position = r.Name
                        };


            var filterPredicate = LinqIntentExpressionBuilder.BuildPredicate<UserDataGridDTO>(intent.Filters);
            int count = await query.CountAsync(filterPredicate);

            query = query.Where(filterPredicate);

            foreach (var sort in intent.Sorts)
            {
                if (sort.Property.Equals("FullName"))
                {
                    if (sort.Direction.Equals(SortDirectionEnum.Ascending))
                        query = query.OrderBy(n => n.FirstName).ThenBy(n => n.LastName);
                    else
                        query = query.OrderByDescending(n => n.FirstName).ThenByDescending(n => n.LastName);
                }
                else
                    query = query.OrderByProperty(sort.Property, sort.Direction);
            }

            query = query.Skip(intent.Skip);
            query = query.Take(intent.Take);

            List<UserDataGridDTO> data = await query.Distinct().ToListAsync();

            return (data, count);
        });
    }
}
