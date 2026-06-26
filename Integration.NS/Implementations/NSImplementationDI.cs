using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryCounting;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Integration.NS.Implementations.Others;
using Integration.NS.Implementations.Transactions;
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
        services.TryAddTransient<IBusinessAccountIntegration, BusinessAccountIntegration>();
        services.TryAddTransient<IInventoryAdjustmentIntegration, InventoryAdjustmentIntegration>();
        services.TryAddTransient<IEmployeeIntegration, EmployeeIntegration>();

        services.TryAddTransient<INetSuiteApiClientService, NetSuiteApiClientService>();

        // TODO FOR REMOVAL
        services.TryAddTransient<ITransactionTypeIntegration, TransactionTypeIntegration>();
        services.TryAddTransient<ISchoolYearIntegration, SchoolYearIntegration>();
        services.TryAddTransient<ITransferTypeIntegration, TransferTypeIntegration>();

        return services;
    }
}
