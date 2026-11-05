namespace Integration.NS.Helpers;

internal static class SuiteQLFragments
{
    /// <summary>
    /// Join predicate matching an ACTIVE trip-ticket exemption record to the order's
    /// source/destination location pair.
    /// <para>
    /// Deliberately free of order-state conditions (status, ordpicked, transfer category).
    /// Exemption is a property of the location pair, so it must stay resolvable even after an
    /// order advances past the packing filters. Adding packing-state predicates here would make
    /// exemption unresolvable exactly when a stale screen posts, which posts the wrong ship
    /// status.
    /// </para>
    /// <para>
    /// Single-sourced so all packing flows apply one exemption rule.
    /// </para>
    /// </summary>
    public const string TripTicketExemptionJoin =
        "tte.custrecord_dbti_tte_from_location = tl.location AND tte.custrecord_dbti_tte_to_location = t.transferlocation AND tte.isinactive = 'F'";

    /// <summary>
    ///     Alias for the derived table returned by <see cref="PendingPackingLines"/>. Append it
    ///     as the join alias, e.g. <c>$"{SuiteQLFragments.PendingPackingLines()} {SuiteQLFragments.PendingPackingLinesAlias} ON ..."</c>.
    /// </summary>
    public const string PendingPackingLinesAlias = "openpk";

    /// <summary>
    ///     Derived table listing every transaction that still has at least one item line with
    ///     quantity left to pack, aliased <see cref="PendingPackingLinesAlias"/>. <b>Inner-join it
    ///     on the transaction id</b> — an order whose every open line is fully packed has no row
    ///     here and drops out of the result set.
    ///     <para>
    ///     Why this exists: the packing screens used to filter on <c>t.ordpicked = 'F'</c> and
    ///     <c>t.status IN ('B','D','E')</c> alone. <c>ordpicked</c> tracks <i>picking</i>, not
    ///     <i>fulfilling</i>, and the transfer-order status only leaves the B/D/E set when the
    ///     Trip Ticket ships the item fulfillment. The WMS posts packing fulfillments with
    ///     shipStatus <c>B</c> (Packed, not Shipped), so between packing and shipping a fully
    ///     packed order still matched the filter and re-appeared on the list with all its lines
    ///     — which invited operators to pack it again.
    ///     </para>
    ///     <para>
    ///     Why a derived-table join and not <c>EXISTS (... WHERE tl2.transaction = t.id)</c>:
    ///     correlation from a subquery into the outer query's aliases is rejected by NetSuite
    ///     with 400 INVALID_PARAMETER "Invalid or unsupported search" — Rule 1 of
    ///     .opencode/docs/SuiteQL-Shape-Rules.md. Resolving the correlation in the join
    ///     predicate is the proven shape (same as the preferred-bin join).
    ///     </para>
    ///     <para>
    ///     Quantity comparisons use ABS on both sides: transfer-order and VRA lines carry
    ///     negative quantities for the return direction, and the existing packing line scripts
    ///     already read <c>ABS(tl.quantity)</c> as the ordered quantity.
    ///     </para>
    /// </summary>
    /// <param name="packedQuantityColumn">
    ///     The transactionline column holding the packed quantity for the order type being
    ///     filtered: <c>quantitypacked</c> for transfer orders, <c>quantityshiprecv</c> for
    ///     vendor return authorizations (the column their packing line script reads).
    /// </param>
    public static string PendingPackingLines(string packedQuantityColumn = "quantitypacked")
    {
        return $"""
             (SELECT opk.transaction AS packtran
              FROM transactionline opk
              WHERE opk.mainline = 'F'
                AND opk.isclosed = 'F'
                AND ABS(opk.quantity) - ABS(opk.{packedQuantityColumn}) > 0
              GROUP BY opk.transaction) {PendingPackingLinesAlias}
             """;
    }

    /// <summary>
    ///     Join predicate pairing <see cref="PendingPackingLines"/> with the driving transaction
    ///     alias <c>t</c>. Pass it to <c>SuiteQLQueryBuilder.InnerJoin</c> alongside
    ///     <see cref="PendingPackingLines"/>; the hand-written pending-fulfillment scripts spell
    ///     the equivalent <c>JOIN ... ON</c> clause inline to keep the correlation in the join
    ///     predicate (Rule 1).
    /// </summary>
    public const string PendingPackingLinesJoinPredicate = PendingPackingLinesAlias + ".packtran = t.id";

    /// <summary>
    ///     Derived table yielding the preferred bin number per item + location.
    ///     <para>
    ///     Aggregated with MIN because NetSuite does not enforce a single preferredbin
    ///     flag per item/location. An item with more than one preferred bin in a location
    ///     would otherwise duplicate every matching row of the outer query.
    ///     </para>
    ///     <para>
    ///     MIN compares bin numbers lexicographically, so this is a display hint only and
    ///     must not be used to pick a bin. To resolve a bin that can be posted back, select
    ///     the internal id (ibq.bin) instead - see PrefferedBinAssignmentId.
    ///     </para>
    ///     <para>
    ///     Pass a location to scope the derived table. Unscoped, NetSuite materialises every
    ///     row in itembinquantity (item x bin) before joining, which is costly on shared queries.
    ///     </para>
    /// </summary>
    /// <param name="location">Location internal id to scope by, or null to leave unscoped.</param>
    /// <returns>A parenthesised subquery. Append the join alias, e.g. $"... pb".</returns>
    public static string PreferredBin(int? location = null)
    {
        var locationFilter = location.HasValue ? $" AND b.location = {location.Value}" : string.Empty;

        return $"""
             (SELECT ibq.item, b.location, MIN(b.binnumber) AS binnumber
              FROM itembinquantity ibq
              JOIN bin b ON ibq.bin = b.id
              WHERE ibq.preferredbin = 'T'{locationFilter}
              GROUP BY ibq.item, b.location)
             """;
    }

    /// <summary>
    ///     Derived table yielding the preferred vendor per item (and optionally subsidiary).
    ///     One row per item+subsidiary holding preferredvendor = 'T' (subsidiary-scoped
    ///     per the item vendor master). The WMS defaults new STR/RTS lines from this so the
    ///     user sees the master default immediately; a non-destructive NetSuite script
    ///     backfills anything missed on save and never overwrites an override.
    /// </summary>
    /// <param name="subsidiary">Subsidiary internal id to scope by (document owning subsidiary), or null to leave unscoped (all preferred rows).</param>
    /// <returns>A parenthesised subquery exposing item, vendor, subsidiary. Append the join alias, e.g. $"{SuiteQLFragments.PreferredVendor(subsidiary)} pv".</returns>
    public static string PreferredVendor(int? subsidiary = null)
    {
        var subsidiaryFilter = subsidiary.HasValue ? $" AND iv.subsidiary = {subsidiary.Value}" : string.Empty;
        return $"""
             (SELECT iv.item, iv.vendor, iv.subsidiary
              FROM itemvendor iv
              WHERE iv.preferredvendor = 'T'{subsidiaryFilter})
             """;
    }

    /// <summary>
    ///     Derived table yielding the preferred vendor per item for the subsidiary owning
    ///     a given location. Row shape and semantics match <see cref="PreferredVendor(int?)"/>
    ///     with a subsidiary argument.
    ///     <para>
    ///     Exists for callers holding only a location id, which must resolve to one
    ///     subsidiary to avoid joining every preferred row for an item.
    ///     <para>
    ///     The subsidiary filter belongs inside the derived table, as in
    ///     ItemsIntegration. Filtering after the join instead
    ///     (<c>ON pv.item = item.id AND pv.subsidiary = loc.subsidiary</c> against an
    ///     unscoped PreferredVendor) is rejected by NetSuite with
    ///     <c>400 [INVALID_PARAMETER] ... Invalid or unsupported search</c>. That correlation
    ///     has no production precedent in this codebase, so prefer this shape.
    ///     </para>
    ///     </para>
    /// </summary>
    /// <param name="location">Location internal id whose subsidiary scopes the result, or null to leave unscoped (all preferred rows).</param>
    /// <returns>A parenthesised subquery exposing item, vendor, subsidiary. Append the join alias, e.g. $"{SuiteQLFragments.PreferredVendorForLocation(location)} pv".</returns>
    public static string PreferredVendorForLocation(int? location = null)
    {
        if (!location.HasValue) return PreferredVendor();

        return $"""
             (SELECT iv.item, iv.vendor, iv.subsidiary
              FROM itemvendor iv
              WHERE iv.preferredvendor = 'T'
                AND iv.subsidiary = (SELECT l.subsidiary FROM location l WHERE l.id = {location.Value}))
             """;
    }
}