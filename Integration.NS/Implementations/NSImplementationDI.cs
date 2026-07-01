using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryCounting;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Integration.NS.Implementations.Others;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Implementations.Vestigial;
using Integration.NS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Integration.NS.Implementations;

public static class NSImplementationDI
{
    public static IServiceCollection AddNSImplementationsIntegraton(this IServiceCollection services)
    {

        services.TryAddTransient<IReceivingIntegration, ReceivingIntegration>();
        services.TryAddTransient<IInventoryTransferIntegration, InventoryTransferIntegration>();
        services.TryAddTransient<IGoodsReturnIntegration, GoodsReturnIntegration>();
        services.TryAddTransient<IBusinessPartnerIntegration, BusinessPartnerIntegration>();
        services.TryAddTransient<IItemMasterDataIntegration, ItemMasterDataIntegration>();
        services.TryAddTransient<IWarehouseMasterDataIntegration, WarehouseMasterDataIntegration>();
        services.TryAddTransient<IGoodsIssueIntegration, GoodsIssueIntegration>();
        services.TryAddTransient<IGoodsReceiptIntegration, GoodsReceiptIntegration>();
        services.TryAddTransient<IDeliveryIntegration, DeliveryIntegration>();
        services.TryAddTransient<ISalesReturnIntegration, SalesReturnIntegration>();
        services.TryAddTransient<IInventoryCountingIntegration, InventoryCountingIntegration>();
        services.TryAddTransient<IItemsIntegration, ItemsIntegration>();
        services.TryAddTransient<IStockTransferRequestIntegration, StockTransferRequestIntegration>();
        services.TryAddTransient<ILocationIntegration, LocationIntegration>();
        services.TryAddTransient<ISubsidiaryIntegration, SubsidiaryIntegration>();
        services.TryAddTransient<IVendorIntegration, VendorIntegration>();
        services.TryAddTransient<ICustomerIntegration, CustomerIntegration>();
        services.TryAddTransient<IBusinessAccountIntegration, BusinessAccountIntegration>();
        services.TryAddTransient<IInventoryTransferRequestIntegration, InventoryTransferRequestIntegration>();
        services.TryAddTransient<IInventoryAdjustmentIntegration, InventoryAdjustmentIntegration>();
        services.TryAddTransient<ITripTicketIntegration, TripTicketIntegration>();
        services.TryAddTransient<IEmployeeIntegration, EmployeeIntegration>();
        services.TryAddTransient<ISupplierReturnIntegration, SupplierReturnIntegration>();
        services.TryAddTransient<IInventoryIntegration, InventoryIntegration>();

        services.TryAddTransient<INetSuiteApiClientService, NetSuiteApiClientService>();
        services.TryAddTransient<IStockTransferRequestPackingIntegration, StockTransferRequestPackingIntegration>();
        services.TryAddTransient<IReturnPackingIntegration, ReturnPackingIntegration>();
        services.TryAddTransient<IVendorReturnAuthorizationPackingIntegration, VendorReturnAuthorizationPackingIntegration>();

        // TODO FOR REMOVAL
        services.TryAddTransient<ITransactionTypeIntegration, TransactionTypeIntegration>();
        services.TryAddTransient<ISchoolYearIntegration, SchoolYearIntegration>();
        services.TryAddTransient<ITransferTypeIntegration, TransferTypeIntegration>();

        return services;
    }
}
