using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Services;

public class ToastifyService
{
    readonly IJSRuntime _jsRuntime;
    IJSObjectReference _jsObj;
    public ToastifyService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;


    }
    // Returns false when Toastify cannot create the notification.
    public async Task<bool> Success(string Message, int durationMs = 3000)
    {
        if (_jsObj is null)
        {
            _jsObj = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/toastifyPresets.js");
        }
        return await _jsObj.InvokeAsync<bool>("ShowSuccess", Message, durationMs);
    }
    public async Task Error(string Message)
    {
        if (_jsObj is null)
        {
            _jsObj = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/toastifyPresets.js");
        }
        await _jsObj.InvokeVoidAsync("ShowError", Message);
    }
    public async Task Warning(string Message)
    {
        if (_jsObj is null)
        {
            _jsObj = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/toastifyPresets.js");
        }
        await _jsObj.InvokeVoidAsync("ShowWarning", Message);
    }
}
