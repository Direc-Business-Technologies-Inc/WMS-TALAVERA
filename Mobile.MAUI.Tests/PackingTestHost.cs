global using Microsoft.AspNetCore.Components;
global using Radzen;
global using Shared.Libraries.Entities;

using Mobile.MAUI.Services;
using Mobile.MAUI.States;
using Shared.Libraries.ViewModel.TransferOrder;
using Shared.Libraries.ViewModel.Returns;
using Shared.Libraries.ViewModel.VendorReturnAuthorization;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

// Only platform/transport, JS presentation, and Razor-generated inheritance are
// replaced. The linked page, barcode resolver, gate, and action runner are real.
namespace Mobile.MAUI
{
    public static class MauiProgram
    {
        public static class BroadcastService
        {
            public static event EventHandler<string>? BroadcastReceived;
            public static void Send(string value) => BroadcastReceived?.Invoke(null, value);
        }
    }
}

namespace Mobile.MAUI.ViewModel { }

namespace Mobile.MAUI.Services
{
    public sealed class ApiClientService
    {
        public event Action? EndpointChanged;
        public Func<string, object, Task<object?>>? Respond { get; set; }
        public List<(string Url, object Body)> Posts { get; } = [];

        public async Task<ApiResult<T>> Post<T>(string url, object body)
        {
            Posts.Add((url, body));
            var result = Respond is null ? null : await Respond(url, body);
            if (result is ApiResult<T> response) return response;
            if (result is T value) return ApiResult<T>.Succeeded(value);
            return ApiResult<T>.Succeeded(typeof(T) == typeof(bool)
                ? (T)(object)true : Activator.CreateInstance<T>());
        }
    }

    public sealed class ToastifyService
    {
        public List<string> Warnings { get; } = [];
        public List<string> Errors { get; } = [];
        public Task<bool> Success(string message) => Task.FromResult(true);
        public Task Warning(string message) { Warnings.Add(message); return Task.CompletedTask; }
        public Task Error(string message) { Errors.Add(message); return Task.CompletedTask; }
    }

    public sealed class NotAuthorizedException(string message) : Exception(message);
}

namespace Mobile.MAUI.Components.Base
{
    public class BaseComponent : ComponentBase
    {
        [Inject] public NavigationManager NavManager { get; set; } = default!;
        [Inject] public DialogService Dialog { get; set; } = default!;
        [Inject] public ToastifyService Toast { get; set; } = default!;
        [Inject] public ApiClientService Client { get; set; } = default!;
        [Inject] public BarcodeResolver BarcodeResolver { get; set; } = default!;
        [Inject] public ActionFactoryService ActionFactory { get; set; } = default!;
        [Inject] public UnsaveChangesHandlerService UnsaveChangesHandler { get; set; } = default!;
        [Inject] public GlobalStates GlobalState { get; set; } = default!;
        protected void MarkScansDirty() => UnsaveChangesHandler.MarkAsDirty();
        protected void MarkScansClean() => UnsaveChangesHandler.MarkAsClean();
        protected Task RevealLastScanned() => Task.CompletedTask;
        protected Task DisposeLastScannedPanel() => Task.CompletedTask;
    }
}

namespace Mobile.MAUI.Components.Reusables
{
    public partial class ManualEntryDialog : ComponentBase { }
}

namespace Mobile.MAUI.Components.Pages.Packing.TransferOrder
{
    public partial class TransferOrderItemView : Base.BaseComponent
    {
        public List<TransferOrderLineVM> TestGood => GoodTOItems;
        public List<TransferOrderLineVM> TestBad => BadTOItems;
        public List<TransferOrderLineVM> TestDone => DoneTOItems;
        public bool TestVendorCleared => VendorId is null && GoodSelectedLine is null && BadSelectedLine is null;
        public void TestSetVendor(int id) => VendorId = id;
        public void TestSelect(TransferOrderLineVM row) => GoodSelectedLine = row;
        public void TestGoodMode() => ScanState = Enums.CustomEnum.ToggleState.Good;
        public Task TestSave() => SaveScan();
        public Task TestChangeVendor() => ChangeVendor();
        public Task TestRefresh() => Refresh();
        public Task TestScan(string value) => ProcessItemScan(value);
    }
}

namespace Mobile.MAUI.Components.Pages.Packing.Returns
{
    public partial class ReturnsItemView : Base.BaseComponent
    {
        public List<ReturnsLineVM> TestRows => ReturnsItems;
        public List<ReturnsLineVM> TestDisplayed => FilteredReturnsItems;
        public List<ReturnsLineVM> TestReview => ScansNeedingReview;
        public Task TestSave() => SaveScan();
        public Task TestRefresh() => Refresh();
        public Task TestScan(string value) => ProcessItemScan(value);
    }
}

namespace Mobile.MAUI.Components.Pages.Packing.VendorReturnAuthorization
{
    public partial class VendorReturnAuthorizationItemView : Base.BaseComponent
    {
        public List<VendorReturnAuthorizationLineVM> TestGood => GoodVRAItems;
        public List<VendorReturnAuthorizationLineVM> TestBad => BadVRAItems;
        public List<VendorReturnAuthorizationLineVM> TestReview => ScansNeedingReview;
        public void TestGoodMode() => ScanState = Enums.CustomEnum.ToggleState.Good;
        public Task TestSave() => SaveScan();
        public Task TestRefresh() => Refresh();
        public Task TestScan(string value) => ProcessItemScan(value);
    }
}
