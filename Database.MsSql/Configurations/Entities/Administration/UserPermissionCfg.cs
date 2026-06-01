using Domain.Entities.Administration.User.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Administration;

internal class UserPermissionCfg : IEntityTypeConfiguration<UserPermissionDEM>
{
    public void Configure(EntityTypeBuilder<UserPermissionDEM> builder)
    {
        builder.ToTable("USR3");
        builder.HasKey(up => up.Id);
        builder.Property<int>("Id")
               .ValueGeneratedOnAdd();
    }
}
