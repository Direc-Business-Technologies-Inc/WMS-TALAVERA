using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.System;

public class ModulePermissionCfg : IEntityTypeConfiguration<ModulePermissionDEM>
{
    public void Configure(EntityTypeBuilder<ModulePermissionDEM> builder)
    {
        builder.ToTable("OMPR");
        builder.Property(mp => mp.Id)
            .IsRequired();
        builder.HasKey(mp => mp.Id)
            .HasName("PK_OMPR");
        builder.OwnsOne(mp => mp.Permission, permission =>
        {
            permission.Property(p => p.Value)
                .HasColumnName("Permission");
        });
            
    }
}
