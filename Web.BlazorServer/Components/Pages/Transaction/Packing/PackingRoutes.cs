namespace Web.BlazorServer.Components.Pages.Transaction.Packing;

public static class PackingRoutes
{
    public const string Root = "/transactions/delivery/packing";
    public const string StockTransferRequestView = $"{Root}/stock-transfer-request/view";
    public const string CreateItemReceipt = $"{Root}/create-item-receipt";
    public const string ReturnsView = $"{Root}/returns/view";
    public const string CreateReturnsItemReceipt = $"{Root}/returns/create-item-receipt";
    public const string VendorReturnAuthorizationView = $"{Root}/vendor-return-authorization/view";
    public const string CreateVendorReturnAuthorizationItemReceipt = $"{Root}/vendor-return-authorization/create-item-receipt";
}
