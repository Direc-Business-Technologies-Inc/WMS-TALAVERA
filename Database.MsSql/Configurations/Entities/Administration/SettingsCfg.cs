using Domain.Entities.Entities.Administration.User.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.MsSql.Configurations.Entities.Administration
{
    internal class SettingsCfg : IEntityTypeConfiguration<SettingsDEM>
    {
        public void Configure(EntityTypeBuilder<SettingsDEM> builder)
        {
            builder.ToTable("OSTN");
            builder.HasKey(s => s.Code).HasName("PK_OSTN");
        }
    }

}
