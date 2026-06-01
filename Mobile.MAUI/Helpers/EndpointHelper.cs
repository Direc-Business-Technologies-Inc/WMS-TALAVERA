using Mobile.MAUI.ViewModel;

namespace Mobile.MAUI.Helpers;

public static class EndpointHelper
{

    public static string BuildURI(ClientEndpointVM vm)
    {
        if (!string.IsNullOrEmpty(vm.BaseEndpoint) && !vm.BaseEndpoint.StartsWith("/"))
        {
            vm.BaseEndpoint = "/" + vm.BaseEndpoint;
        }
        return $"{vm.Protocol.ToString().ToLower()}://{vm.Server}:{vm.Port ?? ""}{vm.BaseEndpoint ?? ""}";
    }

    public static string BuildPrinterURI(ClientEndpointVM vm)
    {
        return vm.PrinterBaseUri;
    }
}
