using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.BlazorServer.Handlers.Implementations.Administration.Authorization;
using Web.BlazorServer.Handlers.Implementations.Administration.Role;
using Web.BlazorServer.Handlers.Implementations.Administration.User;
using Web.BlazorServer.Handlers.Implementations.Others;
using Web.BlazorServer.Handlers.Implementations.System;
using Web.BlazorServer.Handlers.Implementations.Transaction.Delivery;
using Web.BlazorServer.Handlers.Implementations.Transaction.GoodsIssue;
using Web.BlazorServer.Handlers.Implementations.Transaction.GoodsReceipt;
using Web.BlazorServer.Handlers.Implementations.Transaction.GoodsReturn;
using Web.BlazorServer.Handlers.Implementations.Transaction.InventoryAdjustment;
using Web.BlazorServer.Handlers.Implementations.Transaction.InventoryCounting;
using Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransfer;
// using Web.BlazorServer.Handlers.Implementations.Transaction.InventoryTransferRequest;
using Web.BlazorServer.Handlers.Implementations.Transaction.Packing.STR;
using Web.BlazorServer.Handlers.Implementations.Transaction.Receiving;
using Web.BlazorServer.Handlers.Implementations.Transaction.SalesReturn;
using Web.BlazorServer.Handlers.Implementations.Transaction.StockTransferRequest;
using Web.BlazorServer.Handlers.Repositories.Administration.Authorization;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.Handlers.Repositories.Administration.User;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.Handlers.Repositories.Transaction.Delivery;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsIssue;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReceipt;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReturn;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Handlers.Repositories.Transaction.SalesReturn;
using Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Handlers;

public static class BlazorServerHandlersDI
{
    public static IServiceCollection AddBlazorServerHandlers(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.TryAddTransient<INavigationRouteHandler, NavigationRouteHandler>();

        services.TryAddTransient<IUserManagementHandler, UserManagementHandler>();
        services.TryAddTransient<IRoleManagementHandler, RoleManagementHandler>();
        services.TryAddTransient<IModuleHandler, ModuleHandler>();
        services.TryAddTransient<IDocumentNumberHandler, DocumentNumberHandler>();
        services.TryAddTransient<IAuthorizationHandler, AuthorizationHandler>();
        services.TryAddTransient<IReceivingHandler, ReceivingHandler>();
        services.TryAddTransient<IInventoryTransferHandler, InventoryTransferHandler>();
        services.TryAddTransient<IInventoryCountingHandler, InventoryCountingHandler>();
        services.TryAddTransient<IGoodsReturnHandler, GoodsReturnHandler>();
        services.TryAddTransient<IBusinessPartnerHandler, BusinessPartnerHandler>();
        services.TryAddTransient<IItemMasterDataHandler, ItemMasterDataHandler>();
        services.TryAddTransient<IWarehouseMasterDataHandler, WarehouseMasterDataHandler>();
        services.TryAddTransient<IGoodsReceiptHandler, GoodsReceiptHandler>();
        services.TryAddTransient<IGoodsIssueHandler, GoodsIssueHandler>();
        services.TryAddTransient<ITransactionTypeHandler, TransactionTypeHandler>();
        services.TryAddTransient<ITransferTypeHandler, TransferTypeHandler>();
        services.TryAddTransient<ISchoolYearHandler, SchoolYearHandler>();
        services.TryAddTransient<IDeliveryHandler, DeliveryHandler>();
        services.TryAddTransient<ISalesReturnHandler, SalesReturnHandler>();
        services.TryAddTransient<IItemsHandler, ItemsHandler>();
        services.TryAddTransient<ILocationHandler, LocationHandler>();
        services.TryAddTransient<ISubsidiaryHandler, SubsidiaryHandler>();
        services.TryAddTransient<IVendorHandler, VendorHandler>();
        services.TryAddTransient<ISupplierReturnHandler, SupplierReturnHandler>();
        services.TryAddTransient<ICustomerHandler, CustomerHandler>();
        services.TryAddTransient<IInventoryTransferRequestHandler, InventoryTransferRequestHandler>();
        services.TryAddTransient<IStockTransferRequestPackingHandler, StockTransferRequestPackingHandler>();

        if (environment.IsDevelopment())
        {
            services.TryAddTransient<IStockTransferRequestHandler, StockTransferRequestHandler>();
        }
        services.TryAddTransient<IBusinessAccountHandler, BusinessAccountHandler>();
        services.TryAddTransient<IInventoryAdjustmentHandler, InventoryAdjustmentHandler>();

        return services;
    }
}
