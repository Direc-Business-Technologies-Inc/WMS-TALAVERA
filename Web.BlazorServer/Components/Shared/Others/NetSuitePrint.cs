namespace Web.BlazorServer.Components.Shared.Others;

/// <summary>
/// Builds NetSuite scriptlet printout URLs.
/// Centralizes the script ids and session tokens that were previously duplicated
/// across every consuming component.
/// </summary>
/// <remarks>
/// The scriptlet families currently used by the print-button feature are modelled here:
/// script 1671 (keyed by <c>recordId</c>) and script 2109 (keyed by <c>recid</c>).
/// The packing (1922), inventory counting (1927) and item receipt (1914) scriptlets are
/// intentionally left unmigrated so each can be verified against its existing call site
/// when it is moved.
/// </remarks>
public static class NetSuitePrint
{
    private const string SCRIPTLET_HOST = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl";
    private const string ACCOUNT = "compid=11608969";

    /// <summary>
    /// NetSuite session token for scriptlet 1671. These tokens expire and are rotated by a
    /// NetSuite administrator; a rotation currently requires a code change and redeploy.
    /// </summary>
    private const string TOKEN_SCRIPT_1671 = "AAEJ7tMQ9evIwFEEUifIBokQgQ0jhowAItpfjv5Smu7B76K41lU";

    /// <summary>
    /// NetSuite session token for scriptlet 2109 (Inventory Adjustment printout). Rotates
    /// independently of scriptlet 1671; a rotation currently requires a code change and redeploy.
    /// </summary>
    private const string TOKEN_SCRIPT_2109 = "AAEJ7tMQnbh622zQsmsY1In8-tcyWE4hAn92PUCgIBf3JMXUjpE";

    private static string? Build(int? recordId, string recordType, string extraQuery) => recordId is > 0
        ? $"{SCRIPTLET_HOST}?script=1671&deploy=1&{ACCOUNT}&ns-at={TOKEN_SCRIPT_1671}" +
          $"&recordType={recordType}{extraQuery}&recordId={recordId}"
        : null;

    /// <summary>
    /// Picking-ticket printout for a Transfer Order or Intercompany Transfer Order.
    /// Both record types resolve to the same scriptlet, so one builder covers both.
    /// </summary>
    /// <param name="recordId">NetSuite internal id of the transaction (<c>t.id</c>).</param>
    public static string? TransferOrder(int? recordId) =>
        Build(recordId, "tranferOrder", "&isPickingTicket=true");

    /// <summary>Printout for a Returns to Supplier (Vendor Return Authorization).</summary>
    /// <param name="recordId">NetSuite internal id of the transaction (<c>t.id</c>).</param>
    public static string? VendorReturn(int? recordId) =>
        Build(recordId, "vendorreturnauthorization", "&transactionDefault=true");

    /// <summary>Printout for a Trip Ticket.</summary>
    /// <param name="recordId">NetSuite internal id of the custom record.</param>
    public static string? TripTicket(int? recordId) =>
        Build(recordId, "customrecord_dbti_trip_ticket", "&transactionDefault=false");

    /// <summary>
    /// Printout for an Inventory Adjustment goods receipt.
    /// Uses scriptlet 2109, which differs from 1671: its own session token, no record type,
    /// and the id is passed as <c>recid</c> rather than <c>recordId</c>.
    /// </summary>
    /// <param name="recordId">NetSuite internal id of the transaction (<c>t.id</c>).</param>
    public static string? InventoryAdjustmentReceipt(int? recordId) => recordId is > 0
        ? $"{SCRIPTLET_HOST}?script=2109&deploy=1&{ACCOUNT}&ns-at={TOKEN_SCRIPT_2109}&recid={recordId}"
        : null;
}