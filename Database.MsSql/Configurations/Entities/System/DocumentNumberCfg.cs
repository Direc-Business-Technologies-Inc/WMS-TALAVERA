using Domain.Entities.System;
using Domain.Entities.Transaction.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.System;

public class DocumentNumberCfg : IEntityTypeConfiguration<DocumentNumberDEM>
{
    public void Configure(EntityTypeBuilder<DocumentNumberDEM> builder)
    {
        builder.ToTable("ODCN");
        builder.Property(u => u.Id)
            .IsRequired();
        builder.HasKey(u => u.Id)
            .HasName("PK_ODCN");
        builder.Property(u => u.DocumentTypeId)
            .IsRequired();
        builder.Property(u => u.CurrentNumber)
            .IsRequired();
        builder.Property(u => u.NextNumber)
            .IsRequired();
        builder.HasOne<DocumentTypeDEM>()
            .WithOne()
            .HasForeignKey<DocumentNumberDEM>(u => u.DocumentTypeId)
            .HasConstraintName("FK_ODCN_ODCT");
    }
}
