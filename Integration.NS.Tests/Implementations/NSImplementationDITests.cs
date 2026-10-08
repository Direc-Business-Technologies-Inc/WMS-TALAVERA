using Application.UseCases.Repositories.Integration.Others;
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
    public void Reference_integrations_resolve_to_their_cache_decorators(Type serviceType, Type expectedType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService(serviceType);

        Assert.Equal(expectedType, resolved.GetType());
    }

    [Fact]
    public void Transactional_integration_is_not_decorated()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService<IInventoryIntegration>();

        Assert.IsNotType<CachedVendorIntegration>(resolved);
        Assert.Equal("InventoryIntegration", resolved.GetType().Name);
    }
}
