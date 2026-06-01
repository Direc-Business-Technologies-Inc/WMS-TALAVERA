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
    public async Task Success(string Message)
    {
        if (_jsObj is null)
        {
            _jsObj = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/toastifyPresets.js");
        }
        await _jsObj.InvokeVoidAsync("ShowSuccess", Message);
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
