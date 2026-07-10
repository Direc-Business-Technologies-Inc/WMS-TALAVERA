namespace Web.BlazorServer.Services.Repositories
{
    public interface IBusyDialogService
    {
        public void Show(string title = "Loading", string message = "Please wait...");
        public void Hide();
    }
}