using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.ValueObjects.Transaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Transaction.InventoryCounting;

internal class InventoryCountingDocumentCfg : IEntityTypeConfiguration<InventoryCountingDocumentDEM>
{
    public void Configure(EntityTypeBuilder<InventoryCountingDocumentDEM> builder)
    {
        builder.ToTable("OICD");

        builder.HasKey(d => d.Id)
            .HasName("PK_OICD");

        builder.Property(d => d.Id)
            .IsRequired();

        builder.Property(d => d.DocumentTypeId)
            .IsRequired();

        builder.Property(d => d.ApprovalStatus)
            .IsRequired();

        builder.Property(d => d.CountingDate)
            .IsRequired();

        builder.Property(d => d.CycleType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.Remarks)
            .HasMaxLength(500);

        builder.Property(d => d.CreatedBy).IsRequired();
        builder.Property(d => d.CreatedDate).IsRequired();
        builder.Property(d => d.UpdatedBy);
        builder.Property(d => d.UpdatedDate);
        builder.Property(d => d.Archived).IsRequired();

        builder.OwnsOne(d => d.Warehouse, whs =>
        {
            whs.Property(w => w.WhsCode)
                .HasColumnName("WhsCode")
                .HasMaxLength(20)
                .IsRequired();
            whs.Property(w => w.WhsName)
                .HasColumnName("WhsName")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.OwnsOne(d => d.AppDocNum, docNum =>
        {
            docNum.Property(n => n.Value)
                .HasColumnName("AppDocNum")
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.OwnsOne(d => d.SapReference, sap =>
        {
            sap.Property(s => s.DocEntry).HasColumnName("SapDocEntry");
            sap.Property(s => s.DocNum).HasColumnName("SapDocNum");
            sap.Property(s => s.BaseDocEntry).HasColumnName("SapBaseDocEntry");
            sap.Property(s => s.BaseDocNum).HasColumnName("SapBaseDocNum");
        });

        builder.OwnsMany(d => d.DocumentLines, line =>
        {
            line.ToTable("ICD1");
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.WithOwner().HasForeignKey("InventoryCountingDocumentId");
            line.Property(l => l.ItemCode).HasMaxLength(50).IsRequired();
            line.Property(l => l.ItemName).HasMaxLength(200).IsRequired();
            line.Property(l => l.ActualQuantity).IsRequired();
            line.Property(l => l.Quantity).IsRequired();
            line.Property(l => l.UoMCode).HasMaxLength(20).IsRequired();
            line.Property(l => l.UoMValue).IsRequired();
            line.Property(l => l.UoMName).HasMaxLength(100).IsRequired();
            line.Property(l => l.ISBN).HasColumnName("ISBN").HasMaxLength(50);
        });
        builder.Navigation(d => d.DocumentLines)
            .HasField("_documentLines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(d => d.Sheets, sheet =>
        {
            sheet.ToTable("ICD2");
            sheet.Property<int>("Id").ValueGeneratedOnAdd();
            sheet.HasKey("Id");
            sheet.WithOwner().HasForeignKey(s => s.InventoryCountingDocumentId);
            sheet.Property(s => s.InventoryCountingDocumentId).IsRequired();
            sheet.Property(s => s.CounterId).IsRequired();
            sheet.Property(s => s.SubmittedDate).IsRequired();
            sheet.Property(s => s.Status).HasConversion<string>().IsRequired();

            sheet.OwnsOne(s => s.SheetNo, sheetNo =>
            {
                sheetNo.Property(n => n.Value)
                    .HasColumnName("SheetNo")
                    .HasMaxLength(50)
                    .IsRequired();
            });

            sheet.OwnsMany(s => s.SheetLines, sheetLine =>
            {
                sheetLine.ToTable("ICD3");
                sheetLine.Property<int>("Id").ValueGeneratedOnAdd();
                sheetLine.HasKey("Id");
                sheetLine.Property(l => l.ItemCode).HasMaxLength(50).IsRequired();
                sheetLine.Property(l => l.ItemName).HasMaxLength(200).IsRequired();
                sheetLine.Property(l => l.Quantity).IsRequired();
                sheetLine.Property(l => l.UoMCode).HasMaxLength(20).IsRequired();
                sheetLine.Property(l => l.UoMValue).IsRequired();
                sheetLine.Property(l => l.UoMName).HasMaxLength(100).IsRequired();
                sheetLine.Property(l => l.Status).HasConversion<string>().IsRequired();
            });
        });
        builder.Navigation(d => d.Sheets)
            .HasField("_sheets")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
