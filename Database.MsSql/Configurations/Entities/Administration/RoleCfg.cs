using Domain.Entities.Administration.User.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Administration;

internal class RoleCfg : IEntityTypeConfiguration<RoleDEM>
{
    public void Configure(EntityTypeBuilder<RoleDEM> builder)
    {
        builder.ToTable("OROL");
        builder.Property(r => r.Id)
            .IsRequired();
        builder.HasKey(r => r.Id)
            .HasName("PK_OROL");
        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(r => r.RoleId);
    }
}
