using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.System;

public class NavigationRouteCfg : IEntityTypeConfiguration<NavigationRouteDEM>
{
    public void Configure(EntityTypeBuilder<NavigationRouteDEM> builder)
    {
        builder.ToTable("ONRT");
        builder.Property(u => u.Id)
            .IsRequired();
        builder.HasKey(u => u.Id)
            .HasName("PK_ONRT");
    }
}
