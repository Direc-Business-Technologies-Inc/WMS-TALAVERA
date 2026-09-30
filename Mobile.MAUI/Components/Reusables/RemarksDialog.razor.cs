namespace Mobile.MAUI.Components.Reusables;

public partial class RemarksDialog
{
    [Inject] DialogService DialogService { get; set; } = default!;
    private string? Remarks { get; set; }
    private string? ValidationMessage { get; set; }

    private void OnConfirm()
    {
        ValidationMessage = null;

        DialogService.Close(new RemarksResult
        {
            Remarks = Remarks ?? string.Empty,
            IsConfirmed = true
        });
    }

    private void OnSkip()
    {
        DialogService.Close(new RemarksResult
        {
            Remarks = string.Empty,
            IsConfirmed = false
        });
    }

    public class RemarksResult
    {
        public string Remarks { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
    }
}