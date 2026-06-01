using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.System;

public class ModuleCfg : IEntityTypeConfiguration<ModuleDEM>
{
    public void Configure(EntityTypeBuilder<ModuleDEM> builder)
    {
        builder.ToTable("OMDL");
        builder.Property(u => u.Id)
            .IsRequired();
        builder.HasKey(u => u.Id)
            .HasName("PK_OMDL");
        builder.HasOne<NavigationRouteDEM>()
            .WithMany()
            .HasForeignKey(m => m.NavRouteId);
        builder.OwnsMany(m => m.ModulePermissions, permissions =>
        {
            permissions.ToTable("MDL1");
            permissions.WithOwner().HasForeignKey("ModuleId");
            permissions.Property(p => p.Value).HasColumnName("Permission");
        });
    }
}
