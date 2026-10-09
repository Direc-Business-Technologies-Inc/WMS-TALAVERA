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
using Integration.NS.Implementations.Others.Caching;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Implementations.Vestigial;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Application.UseCases.Repositories.Integration.Transaction;
using Shared.Services.Repository;

namespace Integration.NS.Implementations;

public static class NSImplementationDI
{
    /// <summary>Grid-result cache entry cap; the store evicts LRU past this.</summary>
    private const int GridCacheMaxEntries = 500;

    /// <summary>Grid-result TTL (Req 2.1).</summary>
    private static readonly TimeSpan GridCacheTtl = TimeSpan.FromSeconds(60);

    public static IServiceCollection AddNSImplementationsIntegraton(this IServiceCollection services)
    {

        services.TryAddTransient<IInventoryTransferIntegration, InventoryTransferIntegration>();
        services.TryAddTransient<IGoodsReturnIntegration, GoodsReturnIntegration>();
        services.TryAddTransient<IBusinessPartnerIntegration, BusinessPartnerIntegration>();
        services.TryAddTransient<IItemMasterDataIntegration, ItemMasterDataIntegration>();
        services.TryAddTransient<IGoodsIssueIntegration, GoodsIssueIntegration>();
        services.TryAddTransient<IGoodsReceiptIntegration, GoodsReceiptIntegration>();
        services.TryAddTransient<IDeliveryIntegration, DeliveryIntegration>();
        services.TryAddTransient<ISalesReturnIntegration, SalesReturnIntegration>();
        services.TryAddTransient<IInventoryCountingIntegration, InventoryCountingIntegration>();

        // Reference-data reads are wrapped with per-process cache decorators
        // (no DB/Redis in this phase). The concretes are registered so the
        // decorators can wrap them; transactional integrations are untouched.
        services.TryAddTransient<LocationIntegration>();
        services.TryAddTransient<SubsidiaryIntegration>();
        services.TryAddTransient<VendorIntegration>();
        services.TryAddTransient<WarehouseMasterDataIntegration>();
        services.TryAddTransient<DepartmentIntegration>();
        services.TryAddTransient<SchoolYearIntegration>();
        services.TryAddTransient<TransferTypeIntegration>();
        services.TryAddTransient<TransactionTypeIntegration>();
        services.TryAddSingleton<IReferenceDataCacheInvalidator, ReferenceDataCacheInvalidator>();

        services.TryAddTransient<ILocationIntegration>(sp => new CachedLocationIntegration(
            sp.GetRequiredService<LocationIntegration>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<IHttpContextAccessor>()));
        services.TryAddTransient<ISubsidiaryIntegration>(sp => new CachedSubsidiaryIntegration(
            sp.GetRequiredService<SubsidiaryIntegration>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<IHttpContextAccessor>()));
        services.TryAddTransient<IVendorIntegration>(sp => new CachedVendorIntegration(
            sp.GetRequiredService<VendorIntegration>(),
            sp.GetRequiredService<IMemoryCache>()));
        services.TryAddTransient<IWarehouseMasterDataIntegration>(sp => new CachedWarehouseMasterDataIntegration(
            sp.GetRequiredService<WarehouseMasterDataIntegration>(),
            sp.GetRequiredService<IMemoryCache>()));
        services.TryAddTransient<IDepartmentIntegration>(sp => new CachedDepartmentIntegration(
            sp.GetRequiredService<DepartmentIntegration>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<IHttpContextAccessor>()));
        services.TryAddTransient<ISchoolYearIntegration>(sp => new CachedSchoolYearIntegration(
            sp.GetRequiredService<SchoolYearIntegration>(),
            sp.GetRequiredService<IMemoryCache>()));
        services.TryAddTransient<ITransferTypeIntegration>(sp => new CachedTransferTypeIntegration(
            sp.GetRequiredService<TransferTypeIntegration>(),
            sp.GetRequiredService<IMemoryCache>()));
        services.TryAddTransient<ITransactionTypeIntegration>(sp => new CachedTransactionTypeIntegration(
            sp.GetRequiredService<TransactionTypeIntegration>(),
            sp.GetRequiredService<IMemoryCache>()));

        // Live grid reads are wrapped with 60-second, per-user scoped grid
        // caches (Req 2). The NotImplementedException stubs (delivery, goods
        // issue/receipt/return, inventory counting/transfer, sales return) stay
        // undecorated; they are all above.
        services.TryAddSingleton(sp => new GridCacheStore(GridCacheMaxEntries, GridCacheTtl));
        services.TryAddScoped(sp => new GridResultCache(
            sp.GetRequiredService<GridCacheStore>(),
            sp.GetService<IHttpContextAccessor>(),
            sp.GetService<ICurrentUserService>()));
        services.TryAddTransient<ItemsIntegration>();
        services.TryAddTransient<ReceivingIntegration>();
        services.TryAddTransient<StockTransferRequestIntegration>();
        services.TryAddTransient<SupplierReturnIntegration>();
        services.TryAddTransient<TripTicketIntegration>();
        services.TryAddTransient<InventoryAdjustmentIntegration>();
        services.TryAddTransient<InventoryTransferRequestIntegration>();

        services.TryAddTransient<IItemsIntegration>(sp => new CachedItemsIntegration(
            sp.GetRequiredService<ItemsIntegration>(),
            sp.GetRequiredService<GridResultCache>()));
        services.TryAddTransient<IReceivingIntegration>(sp => new CachedReceivingIntegration(
            sp.GetRequiredService<ReceivingIntegration>(),
            sp.GetRequiredService<GridResultCache>()));
        services.TryAddTransient<IStockTransferRequestIntegration>(sp => new CachedStockTransferRequestIntegration(
            sp.GetRequiredService<StockTransferRequestIntegration>(),
            sp.GetRequiredService<GridResultCache>()));
        services.TryAddTransient<ISupplierReturnIntegration>(sp => new CachedSupplierReturnIntegration(
            sp.GetRequiredService<SupplierReturnIntegration>(),
            sp.GetRequiredService<GridResultCache>()));
        services.TryAddTransient<ITripTicketIntegration>(sp => new CachedTripTicketIntegration(
            sp.GetRequiredService<TripTicketIntegration>(),
            sp.GetRequiredService<GridResultCache>()));
        services.TryAddTransient<IInventoryAdjustmentIntegration>(sp => new CachedInventoryAdjustmentIntegration(
            sp.GetRequiredService<InventoryAdjustmentIntegration>(),
            sp.GetRequiredService<GridResultCache>()));
        services.TryAddTransient<IInventoryTransferRequestIntegration>(sp => new CachedInventoryTransferRequestIntegration(
            sp.GetRequiredService<InventoryTransferRequestIntegration>(),
            sp.GetRequiredService<GridResultCache>()));

        services.TryAddTransient<ICustomerIntegration, CustomerIntegration>();
        services.TryAddTransient<IBusinessAccountIntegration, BusinessAccountIntegration>();
        services.TryAddTransient<IEmployeeIntegration, EmployeeIntegration>();
        services.TryAddTransient<IInventoryIntegration, InventoryIntegration>();

        services.TryAddTransient<INetSuiteApiClientService, NetSuiteApiClientService>();
        services.TryAddTransient<IStockTransferRequestPackingIntegration, StockTransferRequestPackingIntegration>();
        services.TryAddTransient<IReturnPackingIntegration, ReturnPackingIntegration>();
        services.TryAddTransient<IVendorReturnAuthorizationPackingIntegration, VendorReturnAuthorizationPackingIntegration>();

        services.TryAddTransient<INetsuiteIdentityIntegration, NetsuiteIdentityIntegration>();

        return services;
    }
}
