using Application.DataTransferObjects.Others.NS;
using Integration.NS.Resilience;
using Xunit;

namespace Integration.NS.Tests.Resilience;

public class NetSuiteRequestClassifierTests
{
    private const string SuiteQlUrl = "https://acct.suitetalk.api.netsuite.com/services/rest/query/v1/suiteql";
    private const string RecordUrl = "https://acct.suitetalk.api.netsuite.com/services/rest/record/v1/transferOrder/1/!transform/itemReceipt";
    private const string RestletUrl = "https://acct.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1862&deploy=1";

    [Fact]
    public void SuiteQL_post_is_a_read()
    {
        Assert.Equal(NetSuiteRequestKind.Read, NetSuiteRequestClassifier.Classify(SuiteQlUrl, HttpMethod.Post));
    }

    [Theory]
    [InlineData("HTTPS://ACCT.suitetalk.api.netsuite.com/services/rest/QUERY/V1/suiteql")]
    [InlineData("https://acct.services/rest/Query/V1/SuiteQL?limit=10")]
    public void SuiteQL_match_is_case_insensitive(string url)
    {
        Assert.Equal(NetSuiteRequestKind.Read, NetSuiteRequestClassifier.Classify(url, HttpMethod.Post));
    }

    [Fact]
    public void Record_post_is_a_write()
    {
        Assert.Equal(NetSuiteRequestKind.Write, NetSuiteRequestClassifier.Classify(RecordUrl, HttpMethod.Post));
    }

    [Fact]
    public void Restlet_post_is_a_write()
    {
        Assert.Equal(NetSuiteRequestKind.Write, NetSuiteRequestClassifier.Classify(RestletUrl, HttpMethod.Post));
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public void Record_mutations_are_writes(string method)
    {
        Assert.Equal(
            NetSuiteRequestKind.Write,
            NetSuiteRequestClassifier.Classify(RecordUrl, new HttpMethod(method)));
    }

    [Fact]
    public void Any_get_is_a_read()
    {
        Assert.Equal(NetSuiteRequestKind.Read, NetSuiteRequestClassifier.Classify(RecordUrl, HttpMethod.Get));
    }
}
