using Domain.Entities.Administration.User.Management;
using Microsoft.EntityFrameworkCore;

namespace Database.MsSql.Configurations.Entities.Administration;

internal class UserLoginCfg : IEntityTypeConfiguration<LoginDEM>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<LoginDEM> builder)
    {
        builder.ToTable("USR2");
        builder.HasKey(l => l.Id);
        builder.Property<int>("Id")
               .ValueGeneratedOnAdd();
    }
}
