using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Domain.Transaction.StockTransferRequest;
using Database.MsSql.Core;
using Domain.Entities.Transaction.StockTransferRequest;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Database.MsSql.Implementation.Transaction.StockTransferRequest;

/// <summary>
/// Uses its own short-lived DbContext: claim writes must commit independently of
/// the existing packing commands' ambient ITransactionalRequest transaction.
/// Expired but owned leases are deliberately NOT stolen automatically: a crashed
/// writer may already have posted an IF and must be reconciled first.
/// </summary>
public sealed class StrAutoFulfillmentStore(IDbContextFactory<AppDbContext> factory) : IStrAutoFulfillmentStore
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(15);

    public async Task<Guid?> TryAcquireAsync(int netsuiteOrderId, CancellationToken ct)
    {
        if (netsuiteOrderId <= 0) return null;
        var owner = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using var db = await factory.CreateDbContextAsync(ct);
        if (await db.StrAutoFulfillmentProcesses.AsNoTracking().AnyAsync(x => x.NetsuiteOrderId == netsuiteOrderId &&
                (x.State == "Posting" || x.State == "OutcomeUncertain" || x.State == "Completed"), ct))
            return null;
        var count = await db.StrFulfillmentLeases
            .Where(x => x.NetsuiteOrderId == netsuiteOrderId && x.OwnerToken == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.OwnerToken, owner)
                .SetProperty(x => x.ExpiresAtUtc, now + LeaseDuration), ct);
        if (count == 1) return owner;

        // A missing row races with another writer's insert; the unique index
        // ensures only one succeeds, and a conflict is a normal failed claim.
        var stale = await db.StrFulfillmentLeases.AsNoTracking()
            .SingleOrDefaultAsync(x => x.NetsuiteOrderId == netsuiteOrderId, ct);
        if (stale is not null)
        {
            if (stale.OwnerToken is null || stale.ExpiresAtUtc is null || stale.ExpiresAtUtc > now)
                return null;
            // A stale lease is reclaimable only if no WMS writer reached its
            // external POST boundary. Posting/uncertain/completed rows are
            // blocked above and must be reconciled by staff.
            var reclaimed = await db.StrFulfillmentLeases.Where(x => x.NetsuiteOrderId == netsuiteOrderId &&
                    x.OwnerToken == stale.OwnerToken && x.ExpiresAtUtc == stale.ExpiresAtUtc)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.OwnerToken, owner)
                    .SetProperty(x => x.ExpiresAtUtc, now + LeaseDuration), ct);
            return reclaimed == 1 ? owner : null;
        }
        try
        {
            db.StrFulfillmentLeases.Add(StrFulfillmentLeaseDEM.New(netsuiteOrderId, owner, now + LeaseDuration));
            await db.SaveChangesAsync(ct);
            return owner;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return null;
        }
    }

    public async Task MarkManualPostingAsync(int netsuiteOrderId, Guid owner, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await OwnsAsync(db, netsuiteOrderId, owner, ct))
            throw new InvalidOperationException("The manual posting claim is no longer owned by this writer.");
        var changed = await db.StrAutoFulfillmentProcesses
            .Where(x => x.NetsuiteOrderId == netsuiteOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Posting")
                .SetProperty(x => x.Reason, "Manual NetSuite fulfillment is in progress.")
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);
        if (changed == 0)
        {
            db.StrAutoFulfillmentProcesses.Add(StrAutoFulfillmentProcessDEM.New(
                netsuiteOrderId, "Posting", DateTime.UtcNow, "Manual NetSuite fulfillment is in progress."));
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task CompleteManualAsync(int netsuiteOrderId, Guid owner, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await OwnsAsync(db, netsuiteOrderId, owner, ct))
            throw new InvalidOperationException("The manual posting claim is no longer owned by this writer.");
        await db.StrAutoFulfillmentProcesses
            .Where(x => x.NetsuiteOrderId == netsuiteOrderId && x.State == "Posting")
            .ExecuteDeleteAsync(ct);
    }

    public async Task<bool> RenewAsync(int netsuiteOrderId, Guid owner, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.StrFulfillmentLeases
            .Where(x => x.NetsuiteOrderId == netsuiteOrderId && x.OwnerToken == owner)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow + LeaseDuration), ct) == 1;
    }

    public async Task ReleaseAsync(int netsuiteOrderId, Guid owner, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.StrFulfillmentLeases
            .Where(x => x.NetsuiteOrderId == netsuiteOrderId && x.OwnerToken == owner)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.OwnerToken, (Guid?)null)
                .SetProperty(x => x.ExpiresAtUtc, (DateTime?)null), ct);
    }

    public async Task<bool> TryStartAutoAsync(int netsuiteOrderId, Guid owner, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await OwnsAsync(db, netsuiteOrderId, owner, ct)) return false;
        var state = await db.StrAutoFulfillmentProcesses.SingleOrDefaultAsync(x => x.NetsuiteOrderId == netsuiteOrderId, ct);
        if (state?.State is "Completed" or "OutcomeUncertain" or "Posting") return false;
        if (state is null)
        {
            db.StrAutoFulfillmentProcesses.Add(StrAutoFulfillmentProcessDEM.New(netsuiteOrderId, "Claimed", DateTime.UtcNow));
            await db.SaveChangesAsync(ct);
            return true;
        }
        await db.StrAutoFulfillmentProcesses.Where(x => x.NetsuiteOrderId == netsuiteOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Claimed")
                .SetProperty(x => x.Reason, (string?)null)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);
        return true;
    }

    public async Task RecordAutoOutcomeAsync(int netsuiteOrderId, Guid owner, string state,
        string? reason, string? fulfillmentIds, CancellationToken ct)
    {
        if (state is not ("Blocked" or "Posting" or "OutcomeUncertain" or "Completed"))
            throw new ArgumentOutOfRangeException(nameof(state));

        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await OwnsAsync(db, netsuiteOrderId, owner, ct))
            throw new InvalidOperationException("The order posting claim is no longer owned by this writer.");
        var changed = await db.StrAutoFulfillmentProcesses
            .Where(x => x.NetsuiteOrderId == netsuiteOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, state)
                .SetProperty(x => x.Reason, reason)
                .SetProperty(x => x.FulfillmentIds, fulfillmentIds)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);
        if (changed != 1) throw new InvalidOperationException("No processing record exists for this order.");
    }

    public async Task RecordDecisionAsync(int netsuiteOrderId, string state, string reason, CancellationToken ct)
    {
        if (netsuiteOrderId <= 0 || state is not ("Skipped" or "ConfigurationException"))
            throw new ArgumentOutOfRangeException(nameof(state));
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.StrAutoFulfillmentProcesses.SingleOrDefaultAsync(x => x.NetsuiteOrderId == netsuiteOrderId, ct);
        if (existing is null)
        {
            try
            {
                db.StrAutoFulfillmentProcesses.Add(StrAutoFulfillmentProcessDEM.New(netsuiteOrderId, state, DateTime.UtcNow, reason));
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Another discovery pass created the result row. Update below.
            }
        }
        await db.StrAutoFulfillmentProcesses.Where(x => x.NetsuiteOrderId == netsuiteOrderId &&
                x.State != "Posting" && x.State != "OutcomeUncertain" && x.State != "Completed")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, state)
                .SetProperty(x => x.Reason, reason)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);
    }

    public async Task MarkManualUncertainAsync(int netsuiteOrderId, Guid owner, string reason, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await OwnsAsync(db, netsuiteOrderId, owner, ct))
            throw new InvalidOperationException("The manual posting claim is no longer owned by this writer.");
        var changed = await db.StrAutoFulfillmentProcesses
            .Where(x => x.NetsuiteOrderId == netsuiteOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "OutcomeUncertain")
                .SetProperty(x => x.Reason, reason)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);
        if (changed == 0)
        {
            var row = StrAutoFulfillmentProcessDEM.New(netsuiteOrderId, "OutcomeUncertain", DateTime.UtcNow, reason);
            db.StrAutoFulfillmentProcesses.Add(row);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<DateTime> GetOrCreateRouteCutoffAsync(string routeKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(routeKey) || routeKey.Length > 128)
            throw new ArgumentException("A valid route key is required.", nameof(routeKey));
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.StrAutoRouteActivations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RouteKey == routeKey, ct);
        if (existing is not null) return existing.ActivatedAtUtc;
        var now = DateTime.UtcNow;
        try
        {
            db.StrAutoRouteActivations.Add(StrAutoRouteActivationDEM.New(routeKey, now));
            await db.SaveChangesAsync(ct);
            return now;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await using var other = await factory.CreateDbContextAsync(ct);
            return (await other.StrAutoRouteActivations.AsNoTracking()
                .SingleAsync(x => x.RouteKey == routeKey, ct)).ActivatedAtUtc;
        }
    }

    public async Task<IReadOnlyList<StrAutoFulfillmentOutcomeDTO>> RecentOutcomesAsync(int take, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rows = await db.StrAutoFulfillmentProcesses.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc).Take(Math.Clamp(take, 1, 200))
            .ToListAsync(ct);
        return rows.Select(x => new StrAutoFulfillmentOutcomeDTO(
            x.NetsuiteOrderId, x.State, x.Reason, x.FulfillmentIds, x.UpdatedAtUtc)).ToArray();
    }

    private static Task<bool> OwnsAsync(AppDbContext db, int orderId, Guid owner, CancellationToken ct) =>
        db.StrFulfillmentLeases.AnyAsync(x => x.NetsuiteOrderId == orderId && x.OwnerToken == owner, ct);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
