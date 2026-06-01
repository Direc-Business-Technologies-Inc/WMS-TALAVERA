using Domain.Entities.Transaction.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.System;

public class DocumentTypeCfg : IEntityTypeConfiguration<DocumentTypeDEM>
{
    public void Configure(EntityTypeBuilder<DocumentTypeDEM> builder)
    {
        builder.ToTable("ODCT");
        builder.Property(u => u.Id)
            .IsRequired();
        builder.HasKey(u => u.Id)
            .HasName("PK_ODCT");
    }
}
