using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Services;

public class ScanService
{
    public event Func<string, Task> OnScanned;
    public void SendScannedValue(string scannedValue)
    {
        OnScanned?.Invoke(scannedValue);
    }

}
