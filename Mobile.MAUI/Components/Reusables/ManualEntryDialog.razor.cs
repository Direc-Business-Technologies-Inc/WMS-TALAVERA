namespace Mobile.MAUI.Components.Reusables;

public partial class ManualEntryDialog
{
    [Inject] DialogService DialogService { get; set; } = default!;

    [Parameter] public string ItemName { get; set; } = string.Empty;
    [Parameter] public decimal PlannedQty { get; set; }
    [Parameter] public int NoBad { get; set; } = 0;


    private bool DontShowBad => NoBad != 1;
    private decimal RemainingQty => PlannedQty - (GoodQty + BadQty);
    private decimal GoodQty { get; set; } = 0;
    private decimal BadQty { get; set; } = 0;
    private string? ValidationMessage { get; set; }

    private void OnConfirm()
    {
        if (GoodQty + BadQty > PlannedQty)
        {
            ValidationMessage = $"Good + Bad Qty cannot exceed Planned Quantity).";
            return;
        }

        if (GoodQty + BadQty <= 0)
        {
            ValidationMessage = "Enter a Good or Bad quantity greater than 0.";
            return;
        }

        DialogService.Close(new ManualEntryResult
        {
            GoodQty = GoodQty,
            BadQty = BadQty
        });
    }

    private void OnCancel()
    {
        DialogService.Close(null);
    }

    public class ManualEntryResult
    {
        public decimal GoodQty { get; set; }
        public decimal BadQty { get; set; }
    }
}
