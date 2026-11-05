namespace Application.DataTransferObjects.Transactions.Commons.NS.Payload;

/// <summary>
/// Single source of truth for the NetSuite inventory status internal ID written to
/// fulfillment/receipt payloads. Every payload builder must resolve through here so the
/// packing and receiving paths cannot drift apart again.
/// </summary>
/// <remarks>
/// These IDs are account configuration, not NetSuite constants. They were verified against
/// the Talavera account on 2026-10-15 with:
/// <c>SELECT id, name FROM inventorystatus WHERE isinactive = 'F'</c>
/// Re-verify before pointing this at another account or subsidiary.
/// </remarks>
public static class InventoryStatusMap
{
    public const string Good = "1";
    public const string Bad = "3";
    public const string Missing = "6";

    /// <summary>Values of <c>custbody_dbti_transfer_category</c>; see the TransferCategory record.</summary>
    public const int ReturnCategoryGood = 3;
    public const int ReturnCategoryBad = 4;

    public static bool IsKnownTransferCategory(int transferCategory) =>
        transferCategory is ReturnCategoryGood or ReturnCategoryBad;

    /// <summary>
    /// Resolves the inventory status for a single packed line.
    /// </summary>
    /// <remarks>
    /// An explicit per-line condition always outranks the order-level transfer category.
    /// That ordering is the whole point: the web packing grid lets an operator mark a line Bad
    /// on a Returns-Good order, and a category-first rule would book that damage as Good stock
    /// with no NetSuite error. Pass <paramref name="transferCategory"/> as null for order types
    /// that have no such field (for example Vendor Return Authorization).
    /// </remarks>
    public static string Resolve(bool isBad, bool isMissing, int? transferCategory = null)
    {
        if (isMissing)
        {
            return Missing;
        }

        if (isBad)
        {
            return Bad;
        }

        return transferCategory == ReturnCategoryBad ? Bad : Good;
    }
}