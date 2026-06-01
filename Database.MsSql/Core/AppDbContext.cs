using Domain.Entities.Administration.User.Management;
using Domain.Entities.Administration.User.Role;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.System;
using Domain.Entities.Transaction.Common;
using Microsoft.EntityFrameworkCore;

namespace Database.MsSql.Core;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    #region DbSets
    public DbSet<ModuleDEM> OMDL { get; set; }
    public DbSet<DocumentTypeDEM> ODCT { get; set; }
    public DbSet<DocumentNumberDEM> ODCN { get; set; }
    public DbSet<NavigationRouteDEM> ONRT { get; set; }
    public DbSet<UserDEM> OUSR { get; set; }
    public DbSet<LoginDEM> USR2 { get; set; }
    public DbSet<RoleDEM> OROL { get; set; }
    public DbSet<UserPermissionDEM> USR3 { get; set; }
    public DbSet<RolePermissionDEM> ROL1 { get; set; }
    public DbSet<ModulePermissionDEM> OMPR { get; set; }
    public DbSet<InventoryCountingDocumentDEM> OICD { get; set; }
    #endregion DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<int>("SN_OUSR")
                    .StartsAt(0)
                    .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
