using Domain.Entities.Transaction.StockTransferRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Transaction;

internal sealed class StrFulfillmentLeaseCfg : IEntityTypeConfiguration<StrFulfillmentLeaseDEM>
{
    public void Configure(EntityTypeBuilder<StrFulfillmentLeaseDEM> builder)
    {
        builder.ToTable("STR_FULFILLMENT_LEASE");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.NetsuiteOrderId).IsUnique();
    }
}
