using Database.MsSql.Configurations;
using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Database.MsSql.Core;

internal class AppDbSeeding
{
    public static async Task SeedData(
        AppDbContext AppDbContext,
        CancellationToken CancellationToken = default
        )
    {
        List<ModulePermissionDEM> ModulePermissions = [];
        IEnumerable<RoleDEM> Roles = AppDefaults.Roles;

        IDbContextTransaction transaction = await AppDbContext.Database.BeginTransactionAsync(CancellationToken);

        try
        {
            if (!await AppDbContext.ONRT.AnyAsync(CancellationToken))
            {
                await AppDbContext.ONRT.AddRangeAsync(WebStructure.ParentRouteList);
                await AppDbContext.ONRT.AddRangeAsync(WebStructure.SubRouteList1);
                await AppDbContext.ONRT.AddRangeAsync(WebStructure.SubRouteList2);
            }

            if (!await AppDbContext.OMDL.AnyAsync(CancellationToken))
                await AppDbContext.OMDL.AddRangeAsync(AppDefaults.ModuleList);

            if (!await AppDbContext.ODCT.AnyAsync(CancellationToken))
                await AppDbContext.ODCT.AddRangeAsync(AppDocuments.List);

            if (!await AppDbContext.OMPR.AnyAsync(CancellationToken))
            {
                IEnumerable<ModuleDEM> modules = await AppDbContext.OMDL.ToListAsync(CancellationToken);

                foreach (var module in AppDefaults.ModuleList)
                    foreach (var permission in module.ModulePermissions)
                        ModulePermissions.Add(ModulePermissionDEM.Create(module.Id, permission));

                await AppDbContext.OMPR.AddRangeAsync(ModulePermissions, CancellationToken);
            }

            if (!await AppDbContext.ODCN.AnyAsync(CancellationToken))
            {
                await AppDbContext.ODCN.AddRangeAsync(AppDocuments.Series, CancellationToken);
            }

            if (!await AppDbContext.OROL.AnyAsync(CancellationToken))
            {
                foreach (var role in Roles)
                {
                    foreach (var module in AppDefaults.RoleModules(role))
                        foreach (var modulePermission in ModulePermissions.Where(mp => mp.ModuleReference == module.Id))
                            role.AddPermission(RolePermissionDEM.Create(modulePermission.Id, role.Id));

                    await AppDbContext.OROL.AddAsync(role);
                }
            }

            if (!await AppDbContext.OUSR.AnyAsync(CancellationToken))
            {

                foreach (var user in AppDefaults.Users)
                {
                    foreach (var permission in Roles.First(r => r.Id == user.RoleId).Permissions)
                        user.AddPermission(UserPermissionDEM.Create(permission.ModulePermission, user.Id));

                    await AppDbContext.OUSR.AddAsync(user);
                }


            }

            await AppDbContext.SaveChangesAsync();
            await transaction.CommitAsync(CancellationToken);

            transaction = await AppDbContext.Database.BeginTransactionAsync(CancellationToken);

            DocumentNumberDEM UserSeries = await AppDbContext.ODCN.FirstAsync(dcn => dcn.DocumentTypeId == AppDocuments.List.ElementAt(0).Id, CancellationToken);
            UserSeries.IncrementNumber();
            UserSeries.IncrementNumber();
            UserSeries.IncrementNumber();

            await AppDbContext.SaveChangesAsync();
            await transaction.CommitAsync(CancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(CancellationToken);
            throw;
        }
    }
}
