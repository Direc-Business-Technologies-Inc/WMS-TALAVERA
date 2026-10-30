using Application.DataTransferObjects.Transactions.StockTransferRequest;

namespace Application.UseCases.Commands.Transaction.StockTransferRequest;

/// <summary>
/// Validates the *complete* NetSuite line set before any item fulfillment POST.
/// This is only a guard: it cannot establish that the upstream NS read really
/// contains all lines unless its caller supplies the authoritative open count.
/// </summary>
public static class AutoFulfillmentPreflight
{
    public static AutoFulfillmentPreflightResultDTO Check(
        IReadOnlyCollection<AutoFulfillmentStockLineDTO> lines,
        int? authoritativeOpenLineCount)
    {
        ArgumentNullException.ThrowIfNull(lines);

        static AutoFulfillmentPreflightResultDTO Block(string reason) =>
            new(AutoFulfillmentPreflightDecision.Blocked, new Dictionary<int, decimal>(), reason);

        if (authoritativeOpenLineCount is null or < 0)
            return Block("The authoritative NetSuite open-line count is unavailable.");

        if (lines.GroupBy(x => x.LineId).Any(x => x.Key <= 0 || x.Count() != 1))
            return Block("NetSuite returned a missing or duplicate order line ID.");

        var open = new List<(AutoFulfillmentStockLineDTO Line, decimal Remaining)>();
        foreach (var line in lines)
        {
            if (line.OrderedBaseQuantity < 0 || line.AlreadyShippedBaseQuantity < 0 ||
                line.AlreadyShippedBaseQuantity > line.OrderedBaseQuantity)
                return Block($"Order line {line.LineId} has inconsistent quantities.");

            if (line.IsClosed || line.OrderedBaseQuantity == line.AlreadyShippedBaseQuantity)
                continue;

            var remaining = line.OrderedBaseQuantity - line.AlreadyShippedBaseQuantity;
            if (line.ItemId <= 0 || line.LocationId <= 0 || line.InventoryStatusId is null or <= 0 ||
                line.RequiresBin is null || (line.RequiresBin == true && line.BinId is null or <= 0) ||
                line.AvailableBaseQuantity is null or < 0 || line.UomRate <= 0)
                return Block($"Order line {line.LineId} lacks verified inventory status, bin, availability or UOM.");

            open.Add((line, remaining));
        }

        if (open.Count != authoritativeOpenLineCount)
            return Block($"NetSuite returned {open.Count} open lines, expected {authoritativeOpenLineCount}.");

        if (open.Count == 0)
            return new(AutoFulfillmentPreflightDecision.NoOpenLines, new Dictionary<int, decimal>(),
                "No open quantities remain for automatic fulfillment.");

        foreach (var pool in open.GroupBy(x =>
                     (x.Line.ItemId, x.Line.LocationId, x.Line.InventoryStatusId, x.Line.BinId)))
        {
            var available = pool.First().Line.AvailableBaseQuantity!.Value;
            if (pool.Any(x => x.Line.AvailableBaseQuantity != available) || pool.Sum(x => x.Remaining) > available)
                return Block($"Available stock is insufficient or inconsistent for item {pool.Key.ItemId} at location {pool.Key.LocationId}.");
        }

        return new(AutoFulfillmentPreflightDecision.Ready,
            open.ToDictionary(x => x.Line.LineId, x => x.Remaining),
            "Every intended open line passed the pre-post inventory checks.");
    }
}
