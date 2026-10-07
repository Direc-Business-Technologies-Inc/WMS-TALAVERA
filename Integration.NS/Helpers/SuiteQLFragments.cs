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
}