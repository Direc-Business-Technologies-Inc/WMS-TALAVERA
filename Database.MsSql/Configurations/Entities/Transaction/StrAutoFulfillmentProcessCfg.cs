using Domain.Entities.Transaction.StockTransferRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Transaction;

internal sealed class StrAutoFulfillmentProcessCfg : IEntityTypeConfiguration<StrAutoFulfillmentProcessDEM>
{
    public void Configure(EntityTypeBuilder<StrAutoFulfillmentProcessDEM> builder)
    {
        builder.ToTable("STR_AUTO_FULFILLMENT_PROCESS");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.NetsuiteOrderId).IsUnique();
        builder.Property(x => x.State).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2048);
        builder.Property(x => x.FulfillmentIds).HasMaxLength(512);
    }
}
