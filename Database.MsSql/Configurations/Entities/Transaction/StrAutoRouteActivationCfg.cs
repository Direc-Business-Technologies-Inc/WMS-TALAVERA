using Domain.Entities.Transaction.StockTransferRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.MsSql.Configurations.Entities.Transaction;

internal sealed class StrAutoRouteActivationCfg : IEntityTypeConfiguration<StrAutoRouteActivationDEM>
{
    public void Configure(EntityTypeBuilder<StrAutoRouteActivationDEM> builder)
    {
        builder.ToTable("STR_AUTO_ROUTE_ACTIVATION");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.RouteKey).IsUnique();
        builder.Property(x => x.RouteKey).HasMaxLength(128).IsRequired();
    }
}
