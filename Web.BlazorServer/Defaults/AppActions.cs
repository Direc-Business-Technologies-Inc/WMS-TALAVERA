using System.ComponentModel;

namespace Web.BlazorServer.Defaults;

public enum AppActions
{
    #region System Actions
    [Description("Get Navigation Routes")]
    GetNavigationRoutes,
    #endregion System Actions

    #region Others
    [Description("Get Customers")]
    GetCustomers,
    [Description("Get Vendors")]
    GetVendors,
    [Description("Get TransactionTypes")]
    GetTransactionTypes,
    [Description("Get Warehouses")]
    GetWarehouses,
    [Description("Get Vendor")]
    GetVendor,
    [Description("Get BusinessPartners")]
    GetBusinessPartners,
    [Description("Get Revenue Streams")]
    GetRevenueStreams,
    [Description("Get Delivery Types")]
    GetDeliveryTypes,
    [Description("Get Items")]
    GetAllItems,
    [Description("Get School Years")]
    GetSchoolYears,
    [Description("Get Purchase Type")]
    GetPurchaseType,
    [Description("Login")]
    Login,
    [Description("Logout")]
    Logout,
    #endregion Others

    #region Administration - Module Management
    [Description("Get All Modules")]
    GetAllModules,
    #endregion Administration - Module Management

    #region Administration - Authorization
    [Description("Get Role Permissions")]
    GetRolePermissions,
    [Description("Get User Permissions")]
    GetUserPermissions,
    [Description("Update User Permissions")]
    UpdateUserPermissions,
    [Description("Update Role Permissions")]
    UpdateRolePermissions,
    [Description("Cascade Role Permissions")]
    CascadeRolePermissions,
    #endregion Administration - Authorization

    #region Administration - User Management
    [Description("Get All Users")]
    GetAllUsers,
    [Description("Create User")]
    CreateUser,
    [Description("View User")]
    ViewUser,
    [Description("Update User")]
    UpdateUser,
    #endregion Administration - User Management

    #region Administration - Role Management
    [Description("Get All Roles")]
    GetAllRoles,
    [Description("Create Role")]
    CreateRole,
    [Description("View Role")]
    ViewRole,
    [Description("Update Role")]
    UpdateRole,
    #endregion Administration - Role Management

    #region Transaction - Receiving
    [Description("Get All Purchase Orders")]
    GetAllPurchaseOrders,
    [Description("Get All Purchase Delivery Notes")]
    GetAllPurchaseDeliveryNotes,
    [Description("Create Goods Receipt PO")]
    CreateGoodsReceiptPO,
    [Description("View Purchase Delivery Note")]
    ViewPurchaseOrder,
    [Description("View Purchase Order")]
    ViewPurchaseDeliveryNote,
    #endregion Transaction - Receiving

    #region Transaction - Goods Return
    [Description("Get Return Types")]
    GetReturnTypes,
    [Description("Get All Goods Return Requests")]
    GetAllGoodsReturnRequests,
    [Description("Get All Goods Returns")]
    GetAllGoodsReturns,
    [Description("Create Goods Return")]
    CreateGoodsReturn,
    [Description("View Goods Return Request")]
    ViewGoodsReturnRequest,
    [Description("View Goods Return")]
    ViewGoodsReturn,
    #endregion Transaction - Goods Return

    #region Transaction - Goods Receipt
    [Description("Get All Goods Receipts")]
    GetAllGoodsReceipts,
    [Description("Create Goods Receipt")]
    CreateGoodsReceipt,
    [Description("View Goods Receipt Request")]
    ViewGoodsReceiptRequest,
    [Description("View Goods Receipt")]
    ViewGoodsReceipt,
    #endregion Transaction - Goods Receipt

    #region Transaction - Delivery
    [Description("Get All Sales Orders")]
    GetAllSalesOrders,
    [Description("Get All Deliveries")]
    GetAllDeliveries,
    [Description("View Sales Order")]
    ViewSalesOrder,
    [Description("View Delivery")]
    ViewDelivery,
    [Description("Create Delivery")]
    CreateDelivery,
    [Description("Get Delivery Means")]
    GetDeliveryMeans,
    #endregion Transaction - Delivery

    #region Transaction - Sales Return
    [Description("Get All Sales Returns")]
    GetAllSalesReturns,
    [Description("Get All Sales Return Requests")]
    GetAllSalesReturnRequests,
    [Description("Create Sales Return")]
    CreateSalesReturn,
    [Description("View Sales Return")]
    ViewSalesReturn,
    [Description("View Sales Return Request")]
    ViewSalesReturnRequest,
    [Description("Get Sales Return Types")]
    GetSalesReturnTypes,
    #endregion Transaction - Sales Return

    #region Transaction - Goods Issue
    [Description("Get All Goods Issues")]
    GetAllGoodsIssues,
    [Description("Create Goods Issue")]
    CreateGoodsIssue,
    [Description("View Goods Issue Request")]
    ViewGoodsIssueRequest,
    [Description("View Goods Issue")]
    ViewGoodsIssue,
    #endregion Transaction - Goods Issue

    #region Inventory Transfer
    [Description("Get All Inventory Transfer Requests")]
    GetAllInventoryTransferRequests,
    [Description("View Inventory Transfer Request")]
    ViewInventoryTransferRequest,
    [Description("Get All Posted Inventory Transfer Requests")]
    GetAllPostedInventoryTransferRequests,
    [Description("View Posted Inventory Transfer Request")]
    ViewPostedInventoryTransferRequest,
    [Description("Get All Pending Inventory Transfer Requests")]
    GetAllPendingInventoryTransferRequests,
    [Description("View Pending Inventory Transfer Request")]
    ViewPendingInventoryTransferRequest,
    [Description("Get All Rejected Inventory Transfer Requests")]
    GetAllRejectedInventoryTransferRequests,
    [Description("View Rejected Inventory Transfer Request")]
    ViewRejectedInventoryTransferRequest,
    [Description("Get Transfer Types")]
    GetTransferTypes,
    [Description("Create Inventory Transfer Request")]
    CreateInventoryTransferRequest,

    #endregion Inventory Transfer

    #region Transaction - Inventory Counting
    [Description("Get All Inventory Counting Documents")]
    GetAllInventoryCountingDocuments,
    [Description("Create Inventory Counting Document")]
    CreateInventoryCountingDocument,
    [Description("View Inventory Counting Document")]
    ViewInventoryCountingDocument,
    [Description("Save Inventory Counting Document")]
    SaveInventoryCountingDocument,
    [Description("Post Inventory Counting Document")]
    PostInventoryCountingDocument,
    [Description("Recount Inventory Counting Document")]
    RecountInventoryCountingDocument,
    [Description("Create Inventory Counting Sheet")]
    CreateInventoryCountingSheet,
    [Description("Ignore Inventory Counting Sheet")]
    IgnoreInventoryCountingSheet,
    [Description("Sync Inventory Counting Sheet")]
    SyncInventoryCountingSheet,
    [Description("Get Warehouse Items for Counting")]
    GetWarehouseItemsForCounting,
    #endregion Transaction - Inventory Counting
}