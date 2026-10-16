using Integration.NS.Implementations.Others.Caching;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Caching;

public class NsCacheKeyTests
{
    [Fact]
    public void Identical_inputs_produce_identical_keys()
    {
        var intent = new DataGridIntent { Take = 10 };

        var first = NsCacheKey.Build("Location", "GetLocationsAsync", intent, null, 1);
        var second = NsCacheKey.Build("Location", "GetLocationsAsync", intent, null, 1);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Key_has_the_parent_shape()
    {
        var key = NsCacheKey.Build("Location", "GetLocationsAsync", new DataGridIntent(), null);

        Assert.StartsWith("ns:cache:v1:Location:GetLocationsAsync:", key);
    }

    [Fact]
    public void Different_method_produces_a_different_key()
    {
        var intent = new DataGridIntent { Take = 10 };

        Assert.NotEqual(
            NsCacheKey.Build("Location", "GetLocationsAsync", intent),
            NsCacheKey.Build("Location", "GetLocation", intent));
    }

    [Fact]
    public void Different_intent_produces_a_different_key()
    {
        Assert.NotEqual(
            NsCacheKey.Build("Location", "GetLocationsAsync", new DataGridIntent { Take = 10 }),
            NsCacheKey.Build("Location", "GetLocationsAsync", new DataGridIntent { Take = 20 }));
    }

    [Fact]
    public void Different_employee_scope_produces_a_different_key()
    {
        var intent = new DataGridIntent { Take = 10 };

        Assert.NotEqual(
            NsCacheKey.Build("Location", "GetUserAllowedLocations", intent, "42"),
            NsCacheKey.Build("Location", "GetUserAllowedLocations", intent, "43"));
    }

    [Fact]
    public void Different_scalar_parameter_produces_a_different_key()
    {
        Assert.NotEqual(
            NsCacheKey.Build("Vendor", "GetVendorsBySubsidiaryListAsync", new DataGridIntent(), null, 1),
            NsCacheKey.Build("Vendor", "GetVendorsBySubsidiaryListAsync", new DataGridIntent(), null, 2));
    }
}
