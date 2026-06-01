using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.ViewModel;

public class ClientEndpointVM
{

    public string Name { get; set; } = string.Empty;
    public string BaseEndpoint { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public Protocol Protocol { get; set; }
    public bool Active { get; set; } = true;
    public string PrinterBaseUri { get; set; } = string.Empty;
    public string DefaultPrinter { get; set; } = string.Empty;
    public List<string> Printers { get; set; } = new List<string>();
}

public enum Protocol
{
    [Description("http")]
    HTTP = 0,
    [Description("https")]
    HTTPS = 1
}
