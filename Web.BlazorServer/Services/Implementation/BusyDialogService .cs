using Radzen;
using Web.BlazorServer.Components.Shared.Others;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation
{
    public class BusyDialogService : IBusyDialogService
    {

        private readonly DialogService _dialogService;
        private bool _isOpen;

        public BusyDialogService(DialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public void Show(string title = "Please Wait", string message = "Processing...")
        {
            if (_isOpen) return;

            _isOpen = true;

            _dialogService.Open<BusyDialog>(
                "",
                new Dictionary<string, object?> //parameters for the BusyDialog
                {
                    { nameof(BusyDialog.Title), title },
                    { nameof(BusyDialog.Message), message }
                },
                options: new DialogOptions()
                {
                    ShowTitle = false,
                    CloseDialogOnOverlayClick = false,
                    ShowClose = false,
                    CssClass = "busy-dialog-container"
                });
        }

        public void Hide()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _dialogService.Close();
        }
    }
}