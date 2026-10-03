using Application.DataTransferObjects.Transactions.Packing;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Integration.NS.DataTransferObjects.Packing;
using Integration.NS.Services;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal sealed class ItemFulfillmentStatusResolver(
    INetSuiteApiClientService netSuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IItemFulfillmentStatusResolver
{
    public async Task<ItemFulfillmentShipStatus> ResolveAsync(
        int sourceTransactionId,
        CancellationToken cancellationToken = default)
    {
        if (sourceTransactionId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sourceTransactionId),
                sourceTransactionId,
                "A valid NetSuite source transaction ID is required.");

        cancellationToken.ThrowIfCancellationRequested();

        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(TripTicketExemptionLookupNSDTO.SourceTransactionId)),
                ("NVL(tte.id, 0)", nameof(TripTicketExemptionLookupNSDTO.TripTicketExemptionId)))
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id")
            .LeftJoin(
                "customrecord_dbti_trip_ticket_exemption tte",
                "tte.custrecord_dbti_tte_from_location = tl.location " +
                "AND tte.custrecord_dbti_tte_to_location = t.transferlocation " +
                "AND tte.isinactive = 'F'")
            .WithFilters(
                Equal("t.id", sourceTransactionId),
                Equal("tl.mainline", "T"))
            .Build();

        var response = await netSuiteService.ExecuteSuiteQLQuery<TripTicketExemptionLookupNSDTO>(
            query.Query,
            query.Limit,
            query.Offset);

        cancellationToken.ThrowIfCancellationRequested();

        if (response.items.Count == 0)
            throw new InvalidOperationException(
                $"NetSuite source transaction {sourceTransactionId} was not found.");

        var exemptionIds = response.items
            .Select(item => item.TripTicketExemptionId)
            .Where(id => id > 0)
            .Distinct()
            .Take(2)
            .ToArray();

        if (exemptionIds.Length > 1)
            throw new InvalidOperationException(
                $"NetSuite source transaction {sourceTransactionId} matches multiple active trip ticket exemptions.");

        return exemptionIds.Length == 1
            ? ItemFulfillmentShipStatus.Shipped
            : ItemFulfillmentShipStatus.Packed;
    }
}
