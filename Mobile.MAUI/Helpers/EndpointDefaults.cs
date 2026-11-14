using Mobile.MAUI.ViewModel;

namespace Mobile.MAUI.Helpers;

/// <summary>
/// Default API endpoint used when a device has no endpoint settings saved.
/// </summary>
public static class EndpointDefaults
{
    public static ClientEndpointVM Polaris => new()
    {
        Name = "POLARIS_API",
        Protocol = Protocol.HTTP,
        Server = "192.168.5.27",
        Port = "8768",
        BaseEndpoint = "/api"
    };
}
