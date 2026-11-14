using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Repositories.Integration.Others;
using Microsoft.AspNetCore.Http;

namespace Integration.NS.Helpers;

internal static class TripTicketSubsidiaryGuard
{
    // Complete every ownership check before the caller performs any NetSuite writes.
    public static async Task ValidateAsync(INetSuiteApiClientService client, IHttpContextAccessor context,
        PostTripTicketDTO data, IEnumerable<ItemFulfillmentDTO>? additionalFulfillments = null,
        bool existingTicket = false)
    {
        var subsidiaryId = CurrentSubsidiaryScope.RequireId(context);
        if (data.FromSubsidiary?.NetsuiteSubsidiaryInternalId != subsidiaryId)
            throw new UnauthorizedAccessException("Trip Ticket source subsidiary must match your logged-in subsidiary.");

        if (existingTicket)
        {
            if (data.Id is not > 0)
                throw new ArgumentException("A valid Trip Ticket is required.");
            await ValidateTicketAsync(client, data.Id.Value, subsidiaryId);
        }

        if (data.Parent > 0)
            await ValidateTicketAsync(client, data.Parent, subsidiaryId);

        var fulfillments = data.ItemFulfillments ?? [];
        if (!existingTicket && fulfillments.Count == 0)
            throw new ArgumentException("Please select at least one Item Fulfillment.");

        var ids = fulfillments.Concat(additionalFulfillments ?? []).Select(x => x.NetsuiteOrderInternalId).Distinct().ToArray();
        if (ids.Any(id => id <= 0))
            throw new ArgumentException("A valid Item Fulfillment is required.");

        // Stay below SuiteQL's IN-expression limit and fetch each bounded set in one page.
        foreach (var batch in ids.Chunk(500))
        {
            var query = $"SELECT t.id AS NetsuiteOrderInternalId, t.subsidiary AS NetsuiteFromSubsidiaryInternalId " +
                $"FROM transaction t WHERE t.type = 'ItemShip' AND t.subsidiary = {subsidiaryId} " +
                $"AND t.id IN ({string.Join(",", batch)})";
            var result = await client.ExecuteSuiteQLQuery<OrdersDTO>(query, 500, 0);
            var ownedIds = result.items?.Where(x => x.NetsuiteFromSubsidiaryInternalId == subsidiaryId)
                .Select(x => x.NetsuiteOrderInternalId).ToHashSet() ?? [];
            if (batch.Any(id => !ownedIds.Contains(id)))
                throw new UnauthorizedAccessException("One or more Item Fulfillments are unavailable in your logged-in subsidiary. Reload the selection and try again.");
        }
    }

    private static async Task ValidateTicketAsync(INetSuiteApiClientService client, int ticketId, int subsidiaryId)
    {
        var result = await client.ExecuteSuiteQLQuery<OrdersDTO>(
            $"SELECT tt.id AS NetsuiteOrderInternalId FROM customrecord_dbti_trip_ticket tt " +
            $"WHERE tt.id = {ticketId} AND tt.custrecord_dbti_trt_from_subsidiary = {subsidiaryId}", 1, 0);
        if (result.items?.Any(x => x.NetsuiteOrderInternalId == ticketId) != true)
            throw new UnauthorizedAccessException("Trip Ticket is unavailable in your logged-in subsidiary.");
    }
}
