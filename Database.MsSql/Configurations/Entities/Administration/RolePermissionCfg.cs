using Domain.Entities.Administration.User.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Administration;

internal class RolePermissionCfg : IEntityTypeConfiguration<RolePermissionDEM>
{
    public void Configure(EntityTypeBuilder<RolePermissionDEM> builder)
    {
        builder.ToTable("ROL1");
        builder.HasKey(up => up.Id);
        builder.Property<int>("Id")
               .ValueGeneratedOnAdd();
    }
}
