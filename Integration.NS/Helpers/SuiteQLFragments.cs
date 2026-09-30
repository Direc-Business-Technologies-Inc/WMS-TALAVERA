namespace Integration.NS.Helpers;

internal static class SuiteQLFragments
{
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
}