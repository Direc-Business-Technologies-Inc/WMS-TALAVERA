using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryCounting;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using Integration.NS;
using Integration.NS.Implementations.Others.Caching;
using Integration.NS.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Integration.NS.Tests.Implementations;

public class NSImplementationDITests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddNSServicesIntegraton();

        // Swap the live NetSuite client for a counting fake so resolving the
        // decorators does not require the full database stack.
        services.RemoveAll<INetSuiteApiClientService>();
        services.AddScoped<INetSuiteApiClientService>(_ => new CountingNetSuiteClient());

        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(typeof(ILocationIntegration), typeof(CachedLocationIntegration))]
    [InlineData(typeof(ISubsidiaryIntegration), typeof(CachedSubsidiaryIntegration))]
    [InlineData(typeof(IVendorIntegration), typeof(CachedVendorIntegration))]
    [InlineData(typeof(IWarehouseMasterDataIntegration), typeof(CachedWarehouseMasterDataIntegration))]
    [InlineData(typeof(IDepartmentIntegration), typeof(CachedDepartmentIntegration))]
    [InlineData(typeof(ISchoolYearIntegration), typeof(CachedSchoolYearIntegration))]
    [InlineData(typeof(ITransferTypeIntegration), typeof(CachedTransferTypeIntegration))]
    [InlineData(typeof(ITransactionTypeIntegration), typeof(CachedTransactionTypeIntegration))]
    public void Reference_integrations_resolve_to_their_cache_decorators(Type serviceType, Type expectedType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.Equal(expectedType, resolved.GetType());
    }

    [Theory]
    [InlineData(typeof(IItemsIntegration), "CachedItemsIntegration")]
    [InlineData(typeof(IReceivingIntegration), "CachedReceivingIntegration")]
    [InlineData(typeof(IStockTransferRequestIntegration), "CachedStockTransferRequestIntegration")]
    [InlineData(typeof(ISupplierReturnIntegration), "CachedSupplierReturnIntegration")]
    [InlineData(typeof(ITripTicketIntegration), "CachedTripTicketIntegration")]
    [InlineData(typeof(IInventoryAdjustmentIntegration), "CachedInventoryAdjustmentIntegration")]
    [InlineData(typeof(IInventoryTransferRequestIntegration), "CachedInventoryTransferRequestIntegration")]
    public void Live_grid_integrations_resolve_to_their_cache_decorators(Type serviceType, string expectedTypeName)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.Equal(expectedTypeName, resolved.GetType().Name);
    }

    [Fact]
    public void Grid_cache_store_is_a_shared_singleton()
    {
        using var provider = BuildProvider();
        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        var storeA = scopeA.ServiceProvider.GetRequiredService<GridCacheStore>();
        var storeB = scopeB.ServiceProvider.GetRequiredService<GridCacheStore>();

        Assert.Same(storeA, storeB);
    }

    [Fact]
    public void Transactional_integration_without_grid_reads_is_not_decorated()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService<IInventoryIntegration>();

        Assert.Equal("InventoryIntegration", resolved.GetType().Name);
    }

    // Stubs that throw NotImplementedException stay undecorated.
    [Theory]
    [InlineData(typeof(IDeliveryIntegration), "DeliveryIntegration")]
    [InlineData(typeof(IGoodsIssueIntegration), "GoodsIssueIntegration")]
    [InlineData(typeof(IGoodsReceiptIntegration), "GoodsReceiptIntegration")]
    [InlineData(typeof(IGoodsReturnIntegration), "GoodsReturnIntegration")]
    [InlineData(typeof(ISalesReturnIntegration), "SalesReturnIntegration")]
    [InlineData(typeof(IInventoryCountingIntegration), "InventoryCountingIntegration")]
    [InlineData(typeof(IInventoryTransferIntegration), "InventoryTransferIntegration")]
    public void Not_implemented_stubs_are_not_decorated(Type serviceType, string expectedTypeName)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.Equal(expectedTypeName, resolved.GetType().Name);
    }

    [Fact]
    public void Reference_data_cache_invalidator_resolves()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService<IReferenceDataCacheInvalidator>();

        Assert.IsType<ReferenceDataCacheInvalidator>(resolved);
    }
}
